using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;
        private Dictionary<string, int> customLabelCount = new();

        private Lowerer()
        {
        }

        private BoundLabel GenerateLabel()
        {
            string name = $"Label{++_labelCount}";
            return new BoundLabel(name);
        }

        private BoundLabel GenerateLabel(string name)
        {
            int count;
            if (!customLabelCount.TryGetValue(name, out count))
                count = 1;

            customLabelCount[name] = count + 1;

            string labelName = name + count;
            return new BoundLabel(labelName);
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            BoundStatement result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(function, result));
        }

        private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
        {
            ImmutableArray<BoundStatement>.Builder builder = ImmutableArray.CreateBuilder<BoundStatement>();
            Stack<BoundStatement> stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                BoundStatement current = stack.Pop();

                if (current is BoundBlockStatement block)
                {
                    foreach (BoundStatement s in block.Statements.Reverse())
                        stack.Push(s);
                }
                else
                    builder.Add(current);
            }

            if (function.Type == TypeSymbol.Void)
            {
                if (builder.Count == 0 || CanFallThrough(builder.Last()))
                    Console.WriteLine("Implement return");
                //builder.Add(new BoundReturnStatement(statement.Syntax, null));
            }

            return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
        }

        private static bool CanFallThrough(BoundStatement boundStatement)
        {
            return boundStatement.Kind != BoundNodeKind.ReturnStatement &&
                   boundStatement.Kind != BoundNodeKind.GotoStatement;
        }

        private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            ControlFlowGraph controlFlow = ControlFlowGraph.Create(node);
            HashSet<BoundStatement> reachableStatements = new HashSet<BoundStatement>(
                controlFlow.Blocks.SelectMany(b => b.Statements));

            ImmutableArray<BoundStatement>.Builder builder = node.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
                if (!reachableStatements.Contains(builder[i]) && builder[i] is not BoundEmitterHint)
                    builder.RemoveAt(i);

            return new BoundBlockStatement(node.Syntax, builder.ToImmutable());
        }

        protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            if (node.ConstantValue != null)
                return Literal(node.Syntax, node.ConstantValue.Value);

            return base.RewriteBinaryExpression(node);
        }

        protected override BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            if (node.ConstantValue != null)
                return Literal(node.Syntax, node.ConstantValue.Value);

            return base.RewriteUnaryExpression(node);
        }

        protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            if (node.ConstantValue != null)
                return Literal(node.Syntax, node.ConstantValue.Value);

            return base.RewriteVariableExpression(node);
        }

        protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            if (node.Expression is BoundArraySegmentExpression expression)
                return lowerArraySegment(expression, node.Variable);

            return base.RewriteAssignmentStatement(node);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement is null)
            {
                // if <condition>
                //      <then>
                //
                // ---->
                //
                // gotoFalse <condition> end
                // <then>
                // end:

                BoundLabel endLabel = GenerateLabel("ifEnd");
                BoundBlockStatement result = Block(
                    node.Syntax,
                    GotoFalse(node.Syntax, endLabel, node.Condition),
                    Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockStart),
                    node.ThenStatement,
                    Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockEnd),
                    Label(node.Syntax, endLabel)
                );

                return RewriteStatement(result);
            }
            else
            {
                // if <condition>
                //      <then>
                // else
                //      <else>
                //
                // ---->
                //
                // gotoFalse <condition> else
                // <then>
                // goto end
                // else:
                // <else>
                // end:

                BoundLabel elseLabel = GenerateLabel("else");
                BoundLabel endLabel = GenerateLabel("ifEnd");
                BoundBlockStatement result = Block(
                    node.Syntax,
                    GotoFalse(node.Syntax, elseLabel, node.Condition),
                    Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockStart),
                    node.ThenStatement,
                    Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockEnd),
                    Goto(node.Syntax, endLabel),
                    Label(node.Syntax, elseLabel),
                    getElse(),
                    Label(node.Syntax, endLabel)
                );

                return RewriteStatement(result);

                BoundStatement getElse()
                {
                    if (node.ElseStatement is BoundIfStatement)
                        return node.ElseStatement;
                    else
                        return Block(
                            node.Syntax,
                            Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockStart),
                            node.ElseStatement!,
                            Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockEnd)
                        );
                }
            }
        }

        protected override BoundStatement RewriteSpecialBlockStatement(BoundSpecialBlockStatement node)
        {
            BoundSpecialBlockStatement newNode = (BoundSpecialBlockStatement)base.RewriteSpecialBlockStatement(node);

            // onPlay
            //      <body>
            //
            // ----->
            //
            // gotoTrue <special condition (play sensor, late update, ...) (handeled by emiter)> onTrue
            // goto end
            // onTrue:
            // <body>
            // goto end [rollback] // special goto that doesn't *really* "goto" but for the purposes of ControlFlowGraph does, neccesary because once body is finished the goto end will execute anyway bacause of how the special block blocks work (exec body, exec after)
            // end:

            BoundLabel onTrueLabel = GenerateLabel("onSpecial");
            BoundLabel endLabel = GenerateLabel("end");
            BoundBlockStatement result = Block(
                newNode.Syntax,
                GotoTrue(newNode.Syntax, onTrueLabel, new BoundSpecialBlockCondition(newNode.Syntax, newNode.Type, newNode.ArgumentClause)),
                Goto(newNode.Syntax, endLabel),
                Label(newNode.Syntax, onTrueLabel),
                // Hint(newNode.Syntax, BoundEmitterHint.HintKind.StatementBlockStart), might need to be sooner if there are ref variables, so let emitter handle it
                newNode.Block,
                Hint(newNode.Syntax, BoundEmitterHint.HintKind.StatementBlockEnd),
                RollbackGoto(newNode.Syntax, endLabel),
                Label(newNode.Syntax, endLabel)
            );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            // while <condition>
            //      <body>
            //
            // ----->
            //
            // continue:
            // gotoFalse <condition> break
            // <body>
            // goto continue
            // break:

            BoundBlockStatement result = Block(
                node.Syntax,
                Label(node.Syntax, node.ContinueLabel),
                GotoFalse(node.Syntax, node.BreakLabel, node.Condition),
                Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockStart),
                node.Body,
                Hint(node.Syntax, BoundEmitterHint.HintKind.StatementBlockEnd),
                Goto(node.Syntax, node.ContinueLabel),
                Label(node.Syntax, node.BreakLabel)
            );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue is not null)
            {
                bool condition = (bool)node.Condition.ConstantValue.GetValueOrDefault(TypeSymbol.Bool);
                condition = node.JumpIfTrue ? condition : !condition;
                if (condition)
                    return RewriteStatement(Goto(node.Syntax, node.Label));
                else
                    return RewriteStatement(Nop(node.Syntax));
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        protected override BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
        {
            BoundCompoundAssignmentStatement newNode = (BoundCompoundAssignmentStatement)base.RewriteCompoundAssignmentStatement(node);

            // a <op>= b
            //
            // ---->
            //
            // a = (a <op> b)

            BoundAssignmentStatement result = Assignment(
                newNode.Syntax,
                newNode.Variable,
                Binary(
                    newNode.Syntax,
                    Variable(newNode.Syntax, newNode.Variable),
                    newNode.Op,
                    newNode.Expression
                )
            );

            return result;
        }

        private BoundStatement lowerArraySegment(BoundArraySegmentExpression expression, VariableSymbol arrayVariable, int startIndex = 0)
        {
            BoundArraySegmentExpression node = (BoundArraySegmentExpression)base.RewriteArraySegmentExpression(expression);

            // x = [a, b, c, ...]
            //
            // ---->
            //
            // set(x, 0, a)
            // set(x, 1, b)
            // set(x, 2, c)
            // ...

            ImmutableArray<BoundStatement>.Builder builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Elements.Length);

            TypeSymbol elementType = arrayVariable.Type.InnerType!;

            float index = 0f;
            foreach (BoundExpression element in node.Elements)
            {
                ImmutableArray<BoundExpression> arguments = [
                    new BoundVariableExpression(node.Syntax, arrayVariable),
                    Literal(node.Syntax, index++),
                    element,
                ];

                builder.Add(new BoundExpressionStatement(node.Syntax, new BoundCallExpression(node.Syntax, BuiltinFunctions.Array_Set, new BoundArgumentClause(node.Syntax, [0, 0, 0], arguments), TypeSymbol.Void, elementType)));
            }

            BoundBlockStatement result = new BoundBlockStatement(
                node.Syntax,
                builder.ToImmutable()
            );

            return result;
        }
    }
}
