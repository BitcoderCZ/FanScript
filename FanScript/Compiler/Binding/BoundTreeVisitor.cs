using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols.Variables;

namespace FanScript.Compiler.Binding
{
    internal abstract class BoundTreeVisitor
    {
        protected virtual void Visit(BoundStatement node)
        {
            switch (node)
            {
                case BoundBlockStatement blockStatement:
                    VisitBlockStatement(blockStatement);
                    break;
                case BoundEventStatement eventStatement:
                    VisitEventStatement(eventStatement);
                    break;
                case BoundNopStatement nopStatement:
                    VisitNopStatement(nopStatement);
                    break;
                case BoundPostfixStatement postfixStatement:
                    VisitPostfixStatement(postfixStatement);
                    break;
                case BoundPrefixStatement prefixStatement:
                    VisitPrefixStatement(prefixStatement);
                    break;
                case BoundVariableDeclarationStatement variableDeclarationStatement:
                    VisitVariableDeclaration(variableDeclarationStatement);
                    break;
                case BoundAssignmentStatement assignmentStatement:
                    VisitAssignmentStatement(assignmentStatement);
                    break;
                case BoundCompoundAssignmentStatement compoundAssignmentStatement:
                    VisitCompoundAssignmentStatement(compoundAssignmentStatement);
                    break;
                case BoundIfStatement ifStatement:
                    VisitIfStatement(ifStatement);
                    break;
                case BoundWhileStatement whileStatement:
                    VisitWhileStatement(whileStatement);
                    break;
                case BoundDoWhileStatement doWhileStatement:
                    VisitDoWhileStatement(doWhileStatement);
                    break;

                //case BoundNodeKind.ForStatement:
                //    VisitForStatement((BoundForStatement)node);
                //    break;
                case BoundLabelStatement labelStatement:
                    VisitLabelStatement(labelStatement);
                    break;
                case BoundGotoStatement gotoStatement:
                    VisitGotoStatement(gotoStatement);
                    break;
                case BoundEventGotoStatement eventGotoStatement:
                    VistiEventGotoStatement(eventGotoStatement);
                    break;
                case BoundConditionalGotoStatement conditionalGotoStatement:
                    VisitConditionalGotoStatement(conditionalGotoStatement);
                    break;
                case BoundReturnStatement returnStatement:
                    VisitReturnStatement(returnStatement);
                    break;
                case BoundEmitterHintStatement emitterHintStatement:
                    VisitEmitterHint(emitterHintStatement);
                    break;
                case BoundCallStatement callStatement:
                    VisitCallStatement(callStatement);
                    break;
                case BoundExpressionStatement expressionStatement:
                    VisitExpressionStatement(expressionStatement);
                    break;
                default:
                    throw new UnexpectedBoundNodeException(node);
            }
        }

        protected virtual void VisitBlockStatement(BoundBlockStatement node)
        {
            for (int i = 0; i < node.Statements.Length; i++)
            {
                Visit(node.Statements[i]);
            }
        }

        protected virtual void VisitEventStatement(BoundEventStatement node)
            => VisitBlockStatement(node.Block);

        protected virtual void VisitNopStatement(BoundNopStatement node)
        {
        }

        protected virtual void VisitPostfixStatement(BoundPostfixStatement node)
        {
        }

        protected virtual void VisitPostfixExpression(BoundPostfixExpression node)
        {
        }

        protected virtual void VisitPrefixStatement(BoundPrefixStatement node)
        {
        }

        protected virtual void VisitPrefixExpression(BoundPrefixExpression node)
        {
        }

        protected virtual void VisitVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            if (node.OptionalAssignment is not null)
            {
                Visit(node.OptionalAssignment);
            }
        }

        protected virtual void VisitAssignmentStatement(BoundAssignmentStatement node)
            => VisitExpression(node.Expression);

        protected virtual void VisitCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
            => VisitExpression(node.Expression);

        protected virtual void VisitIfStatement(BoundIfStatement node)
        {
            VisitExpression(node.Condition);
            Visit(node.ThenStatement);
            if (node.ElseStatement is not null)
            {
                Visit(node.ElseStatement);
            }
        }

        protected virtual void VisitWhileStatement(BoundWhileStatement node)
        {
            VisitExpression(node.Condition);
            Visit(node.Body);
        }

        protected virtual void VisitDoWhileStatement(BoundDoWhileStatement node)
        {
            Visit(node.Body);
            VisitExpression(node.Condition);
        }

        protected virtual void VisitLabelStatement(BoundLabelStatement node)
        {
        }

        protected virtual void VisitGotoStatement(BoundGotoStatement node)
        {
        }

        protected virtual void VistiEventGotoStatement(BoundEventGotoStatement node)
        {
            if (node.ArgumentClause is not null)
            {
                VisitArgumentClause(node.ArgumentClause);
            }
        }

        protected virtual void VisitConditionalGotoStatement(BoundConditionalGotoStatement node)
            => VisitExpression(node.Condition);

        protected virtual void VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.Expression is not null)
            {
                VisitExpression(node.Expression);
            }
        }

        protected virtual void VisitEmitterHint(BoundEmitterHintStatement node)
        {
        }

        protected virtual void VisitCallStatement(BoundCallStatement node)
            => VisitArgumentClause(node.ArgumentClause);

        protected virtual void VisitExpressionStatement(BoundExpressionStatement node)
            => VisitExpression(node.Expression);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
        public virtual void VisitExpression(BoundExpression node)
        {
            switch (node)
            {
                case BoundErrorExpression errorExpression:
                    VisitErrorExpression(errorExpression);
                    break;
                case BoundLiteralExpression literalExpression:
                    VisitLiteralExpression(literalExpression);
                    break;
                case BoundVariableExpression variableExpression:
                    VisitVariableExpression(variableExpression);
                    break;
                case BoundUnaryExpression unaryExpression:
                    VisitUnaryExpression(unaryExpression);
                    break;
                case BoundBinaryExpression binaryExpression:
                    VisitBinaryExpression(binaryExpression);
                    break;
                case BoundCallExpression callExpression:
                    VisitCallExpression(callExpression);
                    break;
                case BoundConversionExpression conversionExpression:
                    VisitConversionExpression(conversionExpression);
                    break;
                case BoundConstructorExpression constructorExpression:
                    VisitConstructorExpression(constructorExpression);
                    break;
                case BoundPostfixExpression postfixExpression:
                    VisitPostfixExpression(postfixExpression);
                    break;
                case BoundPrefixExpression prefixExpression:
                    VisitPrefixExpression(prefixExpression);
                    break;
                case BoundArraySegmentExpression arraySegmentExpression:
                    VisitArraySegmentExpression(arraySegmentExpression);
                    break;
                case BoundAssignmentExpression assignmentExpression:
                    VisitAssignmentExpression(assignmentExpression);
                    break;
                case BoundCompoundAssignmentExpression compoundAssignmentExpression:
                    VisitCompoundAssignmentExpression(compoundAssignmentExpression);
                    break;
                default:
                    throw new UnexpectedBoundNodeException(node);
            }
        }

        protected virtual void VisitErrorExpression(BoundErrorExpression node)
        {
        }

        protected virtual void VisitLiteralExpression(BoundLiteralExpression node)
        {
        }

        protected virtual void VisitVariableExpression(BoundVariableExpression node)
        {
            switch (node.Variable)
            {
                case PropertySymbol prop:
                    VisitExpression(prop.Expression);
                    break;
                default:
                    break;
            }
        }

        protected virtual void VisitUnaryExpression(BoundUnaryExpression node)
            => VisitExpression(node.Operand);

        protected virtual void VisitBinaryExpression(BoundBinaryExpression node)
        {
            VisitExpression(node.Left);
            VisitExpression(node.Right);
        }

        protected virtual void VisitCallExpression(BoundCallExpression node)
            => VisitArgumentClause(node.ArgumentClause);

        protected virtual void VisitConversionExpression(BoundConversionExpression node)
            => VisitExpression(node.Expression);

        protected virtual void VisitConstructorExpression(BoundConstructorExpression node)
        {
            VisitExpression(node.ExpressionX);
            VisitExpression(node.ExpressionY);
            VisitExpression(node.ExpressionZ);
        }

        protected virtual void VisitArraySegmentExpression(BoundArraySegmentExpression node)
        {
            for (int i = 0; i < node.Elements.Length; i++)
            {
                VisitExpression(node.Elements[i]);
            }
        }

        protected virtual void VisitAssignmentExpression(BoundAssignmentExpression node)
            => VisitExpression(node.Expression);

        protected virtual void VisitCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
            => VisitExpression(node.Expression);

        #region Helper functions
        protected virtual void VisitArgumentClause(BoundArgumentClause node)
        {
            for (int i = 0; i < node.Arguments.Length; i++)
            {
                VisitExpression(node.Arguments[i]);
            }
        }
        #endregion
    }
}
