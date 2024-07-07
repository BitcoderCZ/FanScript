using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;

        private Lowerer()
        {
        }

        private BoundLabel GenerateLabel()
        {
            string name = $"Label{++_labelCount}";
            return new BoundLabel(name);
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            BoundStatement result = lowerer.RewriteStatement(statement);
            return /*result is BoundBlockStatement block ? block : new BoundBlockStatement(result.Syntax, [result]);*/RemoveDeadCode(Flatten(function, result));
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
                    builder.Add(new BoundReturnStatement(statement.Syntax, null));
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
