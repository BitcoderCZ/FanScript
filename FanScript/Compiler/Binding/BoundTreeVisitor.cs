using FanScript.Compiler.Symbols.Variables;

namespace FanScript.Compiler.Binding
{
    internal abstract class BoundTreeVisitor
    {
        protected virtual void Visit(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    VisitBlockStatement((BoundBlockStatement)node);
                    break;
                case BoundNodeKind.EventStatement:
                    VisitEventStatement((BoundEventStatement)node);
                    break;
                case BoundNodeKind.NopStatement:
                    VisitNopStatement((BoundNopStatement)node);
                    break;
                case BoundNodeKind.PostfixStatement:
                    VisitPostfixStatement((BoundPostfixStatement)node);
                    break;
                case BoundNodeKind.PrefixStatement:
                    VisitPrefixStatement((BoundPrefixStatement)node);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    VisitVariableDeclaration((BoundVariableDeclarationStatement)node);
                    break;
                case BoundNodeKind.AssignmentStatement:
                    VisitAssignmentStatement((BoundAssignmentStatement)node);
                    break;
                case BoundNodeKind.CompoundAssignmentStatement:
                    VisitCompoundAssignmentStatement((BoundCompoundAssignmentStatement)node);
                    break;
                case BoundNodeKind.IfStatement:
                    VisitIfStatement((BoundIfStatement)node);
                    break;
                case BoundNodeKind.WhileStatement:
                    VisitWhileStatement((BoundWhileStatement)node);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    VisitDoWhileStatement((BoundDoWhileStatement)node);
                    break;
                //case BoundNodeKind.ForStatement:
                //    VisitForStatement((BoundForStatement)node);
                //    break;
                case BoundNodeKind.LabelStatement:
                    VisitLabelStatement((BoundLabelStatement)node);
                    break;
                case BoundNodeKind.GotoStatement:
                    VisitGotoStatement((BoundGotoStatement)node);
                    break;
                case BoundNodeKind.RollbackGotoStatement:
                    VisitRollbackGotoStatement((BoundRollbackGotoStatement)node);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    VisitConditionalGotoStatement((BoundConditionalGotoStatement)node);
                    break;
                case BoundNodeKind.ReturnStatement:
                    VisitReturnStatement((BoundReturnStatement)node);
                    break;
                case BoundNodeKind.EmitterHint:
                    VisitEmitterHint((BoundEmitterHint)node);
                    break;
                case BoundNodeKind.CallStatement:
                    VisitCallStatement((BoundCallStatement)node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    VisitExpressionStatement((BoundExpressionStatement)node);
                    break;
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual void VisitBlockStatement(BoundBlockStatement node)
        {
            for (var i = 0; i < node.Statements.Length; i++)
                Visit(node.Statements[i]);
        }

        protected virtual void VisitEventStatement(BoundEventStatement node)
        {
            VisitBlockStatement(node.Block);
        }

        protected virtual void VisitNopStatement(BoundNopStatement node)
        { }

        protected virtual void VisitPostfixStatement(BoundPostfixStatement node)
        { }

        protected virtual void VisitPostfixExpression(BoundPostfixExpression node)
        { }

        protected virtual void VisitPrefixStatement(BoundPrefixStatement node)
        { }

        protected virtual void VisitPrefixExpression(BoundPrefixExpression node)
        { }

        protected virtual void VisitVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            if (node.OptionalAssignment is not null)
                Visit(node.OptionalAssignment);
        }

        protected virtual void VisitAssignmentStatement(BoundAssignmentStatement node)
        {
            VisitExpression(node.Expression);
        }

        protected virtual void VisitCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
        {
            VisitExpression(node.Expression);
        }

        protected virtual void VisitIfStatement(BoundIfStatement node)
        {
            VisitExpression(node.Condition);
            Visit(node.ThenStatement);
            if (node.ElseStatement is not null)
                Visit(node.ElseStatement);
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
        { }

        protected virtual void VisitGotoStatement(BoundGotoStatement node)
        { }

        protected virtual void VisitRollbackGotoStatement(BoundRollbackGotoStatement node)
        { }

        protected virtual void VisitConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition is BoundEventCondition eventCondition)
            {
                if (eventCondition.ArgumentClause is not null)
                    VisitArgumentClause(eventCondition.ArgumentClause);
            }
            else
                VisitExpression(node.Condition);
        }

        protected virtual void VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.Expression is not null)
                VisitExpression(node.Expression);
        }

        protected virtual void VisitEmitterHint(BoundEmitterHint node)
        { }

        protected virtual void VisitCallStatement(BoundCallStatement node)
        {
            VisitArgumentClause(node.ArgumentClause);
        }

        protected virtual void VisitExpressionStatement(BoundExpressionStatement node)
        {
            VisitExpression(node.Expression);
        }

        public virtual void VisitExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    VisitErrorExpression((BoundErrorExpression)node);
                    break;
                case BoundNodeKind.LiteralExpression:
                    VisitLiteralExpression((BoundLiteralExpression)node);
                    break;
                case BoundNodeKind.VariableExpression:
                    VisitVariableExpression((BoundVariableExpression)node);
                    break;
                case BoundNodeKind.UnaryExpression:
                    VisitUnaryExpression((BoundUnaryExpression)node);
                    break;
                case BoundNodeKind.BinaryExpression:
                    VisitBinaryExpression((BoundBinaryExpression)node);
                    break;
                case BoundNodeKind.CallExpression:
                    VisitCallExpression((BoundCallExpression)node);
                    break;
                case BoundNodeKind.ConversionExpression:
                    VisitConversionExpression((BoundConversionExpression)node);
                    break;
                case BoundNodeKind.ConstructorExpression:
                    VisitConstructorExpression((BoundConstructorExpression)node);
                    break;
                case BoundNodeKind.PostfixExpression:
                    VisitPostfixExpression((BoundPostfixExpression)node);
                    break;
                case BoundNodeKind.PrefixExpression:
                    VisitPrefixExpression((BoundPrefixExpression)node);
                    break;
                case BoundNodeKind.ArraySegmentExpression:
                    VisitArraySegmentExpression((BoundArraySegmentExpression)node);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    VisitAssignmentExpression((BoundAssignmentExpression)node);
                    break;
                case BoundNodeKind.CompoundAssignmentExpression:
                    VisitCompoundAssignmentExpression((BoundCompoundAssignmentExpression)node);
                    break;
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual void VisitErrorExpression(BoundErrorExpression node)
        { }

        protected virtual void VisitLiteralExpression(BoundLiteralExpression node)
        { }

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
        {
            VisitExpression(node.Operand);
        }

        protected virtual void VisitBinaryExpression(BoundBinaryExpression node)
        {
            VisitExpression(node.Left);
            VisitExpression(node.Right);
        }

        protected virtual void VisitCallExpression(BoundCallExpression node)
        {
            VisitArgumentClause(node.ArgumentClause);
        }

        protected virtual void VisitConversionExpression(BoundConversionExpression node)
        {
            VisitExpression(node.Expression);
        }

        protected virtual void VisitConstructorExpression(BoundConstructorExpression node)
        {
            VisitExpression(node.ExpressionX);
            VisitExpression(node.ExpressionY);
            VisitExpression(node.ExpressionZ);
        }

        protected virtual void VisitArraySegmentExpression(BoundArraySegmentExpression node)
        {
            for (var i = 0; i < node.Elements.Length; i++)
                VisitExpression(node.Elements[i]);
        }

        protected virtual void VisitAssignmentExpression(BoundAssignmentExpression node)
        {
            VisitExpression(node.Expression);
        }

        protected virtual void VisitCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            VisitExpression(node.Expression);
        }

        #region Helper functions
        protected virtual void VisitArgumentClause(BoundArgumentClause node)
        {
            for (var i = 0; i < node.Arguments.Length; i++)
                VisitExpression(node.Arguments[i]);
        }
        #endregion
    }
}
