using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)node);
                case BoundNodeKind.SpecialBlockStatement:
                    return RewriteSpecialBlockStatement((BoundSpecialBlockStatement)node);
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement)node);
                case BoundNodeKind.VariableDeclaration:
                    return RewriteVariableDeclaration((BoundVariableDeclaration)node);
                case BoundNodeKind.AssignmentStatement:
                    return RewriteAssignmentStatement((BoundAssignmentStatement)node);
                case BoundNodeKind.CompoundAssignmentStatement:
                    return RewriteCompoundAssignmentStatement((BoundCompoundAssignmentStatement)node);
                case BoundNodeKind.ArrayInitializerStatement:
                    return RewriteArrayInitializerStatement((BoundArrayInitializerStatement)node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)node);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement)node);
                //case BoundNodeKind.DoWhileStatement:
                //    return RewriteDoWhileStatement((BoundDoWhileStatement)node);
                //case BoundNodeKind.ForStatement:
                //    return RewriteForStatement((BoundForStatement)node);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement)node);
                case BoundNodeKind.GotoStatement:
                    return RewriteGotoStatement((BoundGotoStatement)node);
                case BoundNodeKind.RollbackGotoStatement:
                    return RewriteRollbackGotoStatement((BoundRollbackGotoStatement)node);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node);
                case BoundNodeKind.ReturnStatement:
                //    return RewriteReturnStatement((BoundReturnStatement)node);
                case BoundNodeKind.EmitterHint:
                    return RewriteEmitterHint((BoundEmitterHint)node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;

            for (var i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement oldStatement = node.Statements[i];
                BoundStatement newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);

                        for (var j = 0; j < i; j++)
                            builder.Add(node.Statements[j]);
                    }
                }

                if (builder is not null)
                    builder.Add(newStatement);
            }

            if (builder is null)
                return node;

            return new BoundBlockStatement(node.Syntax, builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteSpecialBlockStatement(BoundSpecialBlockStatement node)
        {
            BoundArgumentClause argumentClause = RewriteArgumentClause(node.ArgumentClause);

            BoundBlockStatement block = (BoundBlockStatement)RewriteBlockStatement(node.Block);

            if (argumentClause == node.ArgumentClause && block == node.Block)
                return node;

            return new BoundSpecialBlockStatement(node.Syntax, node.Type, argumentClause, block);
        }

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node)
            => node;

        protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclaration node)
        {
            if (node.OptionalAssignment is null)
                return node;
            else if (node.OptionalAssignment is BoundArrayInitializerStatement arrayInitializer)
                return new BoundBlockStatement(
                    node.Syntax,
                    [
                        new BoundVariableDeclaration(node.Syntax, node.Variable, null),
                        RewriteArrayInitializerStatement(arrayInitializer)
                    ]);

            BoundStatement assignment = RewriteStatement(node.OptionalAssignment);
            if (assignment == node.OptionalAssignment)
                return node;

            return new BoundVariableDeclaration(node.Syntax, node.Variable, assignment);
        }

        protected virtual BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentStatement(node.Syntax, node.Variable, expression);
        }

        protected virtual BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundCompoundAssignmentStatement(node.Syntax, node.Variable, node.Op, expression);
        }

        protected virtual BoundStatement RewriteArrayInitializerStatement(BoundArrayInitializerStatement node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (var i = 0; i < node.Elements.Length; i++)
            {
                BoundExpression oldElement = node.Elements[i];
                BoundExpression newElement = RewriteExpression(oldElement);
                if (newElement != oldElement)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Elements.Length);

                        for (int j = 0; j < i; j++)
                            builder.Add(node.Elements[j]);
                    }
                }

                if (builder is not null)
                    builder.Add(newElement);
            }

            if (builder is null)
                return node;

            return new BoundArrayInitializerStatement(node.Syntax, node.Variable, builder.ToImmutable());
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = node.ElseStatement is null ? null : RewriteStatement(node.ElseStatement);
            if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
                return node;

            return new BoundIfStatement(node.Syntax, condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
            => node;

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
            => node;

        protected virtual BoundStatement RewriteRollbackGotoStatement(BoundRollbackGotoStatement node)
            => node;

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition is BoundSpecialBlockCondition)
                return node;

            BoundExpression condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteEmitterHint(BoundEmitterHint node)
            => node;

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(node.Syntax, expression);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression)node);
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression)node);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression)node);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression)node);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression)node);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression)node);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression)node);
                case BoundNodeKind.ConstructorExpression:
                    return RewriteConstructorExpression((BoundConstructorExpression)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
            => node;

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
            => node;

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
            => node;

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression operand = RewriteExpression(node.Operand);

            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Syntax, node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression left = RewriteExpression(node.Left);
            BoundExpression right = RewriteExpression(node.Right);

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(node.Syntax, left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            BoundArgumentClause argumentClause = RewriteArgumentClause(node.ArgumentClause);

            if (argumentClause == node.ArgumentClause)
                return node;

            return new BoundCallExpression(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType);
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);

            if (expression == node.Expression)
                return node;

            return new BoundConversionExpression(node.Syntax, node.Type, expression);
        }

        protected virtual BoundExpression RewriteConstructorExpression(BoundConstructorExpression node)
        {
            BoundExpression expressionX = RewriteExpression(node.ExpressionX);
            BoundExpression expressionY = RewriteExpression(node.ExpressionY);
            BoundExpression expressionZ = RewriteExpression(node.ExpressionZ);

            bool xDiff = expressionX != node.ExpressionX,
                yDiff = expressionY != node.ExpressionY,
                zDiff = expressionZ != node.ExpressionZ;

            if (xDiff || yDiff || zDiff)
                return new BoundConstructorExpression(node.Syntax, node.Type,
                    xDiff ? expressionX : node.ExpressionX,
                    yDiff ? expressionY : node.ExpressionY,
                    zDiff ? expressionZ : node.ExpressionZ
                );
            else
                return node;
        }

        protected virtual BoundArgumentClause RewriteArgumentClause(BoundArgumentClause node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (var i = 0; i < node.Arguments.Length; i++)
            {
                BoundExpression oldArgument = node.Arguments[i];
                BoundExpression newArgument = RewriteExpression(oldArgument);
                if (newArgument != oldArgument)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);

                        for (int j = 0; j < i; j++)
                            builder.Add(node.Arguments[j]);
                    }
                }

                if (builder is not null)
                    builder.Add(newArgument);
            }

            if (builder is null)
                return node;

            return new BoundArgumentClause(node.Syntax, node.ArgModifiers, builder.ToImmutable());
        }
    }
}
