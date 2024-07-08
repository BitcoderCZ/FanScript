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
                if (!reachableStatements.Contains(builder[i]))
                    builder.RemoveAt(i);

            return new BoundBlockStatement(node.Syntax, builder.ToImmutable());
        }

        protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            if (node.ConstantValue != null)
                return Literal(node.Syntax, node.ConstantValue.Value);

            return node;
        }

        protected override BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            if (node.ConstantValue != null)
                return Literal(node.Syntax, node.ConstantValue.Value);

            return node;
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement == null)
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
                    node.ThenStatement,
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
                var result = Block(
                    node.Syntax,
                    GotoFalse(node.Syntax, elseLabel, node.Condition),
                    node.ThenStatement,
                    Goto(node.Syntax, endLabel),
                    Label(node.Syntax, elseLabel),
                    node.ElseStatement,
                    Label(node.Syntax, endLabel)
                );

                return RewriteStatement(result);
            }
        }

        protected override BoundStatement RewriteSpecialBlockStatement(BoundSpecialBlockStatement node)
        {
            // onPlay
            //      <body>
            //
            // ----->
            //
            // gotoTrue <onPlay (handeled by emiter)> onTrue
            // goto end
            // onTrue:
            // <body>
            // goto end [rollback] // special "label" that doesn't really exist, neccesary because once body is finished the goto end will execute anyway bacause of how the special block blocks work (exec body, exec after)
            // end:

            BoundLabel onTrueLabel = GenerateLabel("onSpecial");
            BoundLabel endLabel = GenerateLabel("end");
            BoundBlockStatement result = Block(
                node.Syntax,
                GotoTrue(node.Syntax, onTrueLabel, new BoundSpecialBlockCondition(node.Keyword, node.Keyword.Kind)),
                Goto(node.Syntax, endLabel),
                Label(node.Syntax, onTrueLabel),
                node.Block,
                RollbackGoto(node.Syntax, endLabel),
                Label(node.Syntax, endLabel)
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
                node.Body,
                Goto(node.Syntax, node.ContinueLabel),
                Label(node.Syntax, node.BreakLabel)
            );

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue is not null)
            {
                bool condition = (bool)node.Condition.ConstantValue.Value;
                condition = node.JumpIfTrue ? condition : !condition;
                if (condition)
                    return RewriteStatement(Goto(node.Syntax, node.Label));
                else
                    return RewriteStatement(Nop(node.Syntax));
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            BoundCompoundAssignmentExpression newNode = (BoundCompoundAssignmentExpression)base.RewriteCompoundAssignmentExpression(node);

            // a <op>= b
            //
            // ---->
            //
            // a = (a <op> b)

            BoundAssignmentExpression result = Assignment(
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
    }
}
