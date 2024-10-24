using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols.Variables;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding.Rewriters
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            return node switch
            {
                BoundBlockStatement blockStatement => RewriteBlockStatement(blockStatement),
                BoundEventStatement eventStatement => RewriteEventStatement(eventStatement),
                BoundNopStatement nopStatement => RewriteNopStatement(nopStatement),
                BoundPostfixStatement postfixStatement => RewritePostfixStatement(postfixStatement),
                BoundPrefixStatement prefixStatement => RewritePrefixStatement(prefixStatement),
                BoundVariableDeclarationStatement variableDeclarationStatement => RewriteVariableDeclaration(variableDeclarationStatement),
                BoundAssignmentStatement assignmentStatement => RewriteAssignmentStatement(assignmentStatement),
                BoundCompoundAssignmentStatement compoundAssignmentStatement => RewriteCompoundAssignmentStatement(compoundAssignmentStatement),
                BoundIfStatement ifStatement => RewriteIfStatement(ifStatement),
                BoundWhileStatement whileStatement => RewriteWhileStatement(whileStatement),
                BoundDoWhileStatement doWhileStatement => RewriteDoWhileStatement(doWhileStatement),
                //case BoundNodeKind.ForStatement:
                //    return RewriteForStatement((BoundForStatement)node);
                BoundLabelStatement labelStatement => RewriteLabelStatement(labelStatement),
                BoundGotoStatement gotoStatement => RewriteGotoStatement(gotoStatement),
                BoundEventGotoStatement eventGotoStatement => RewriteEventGotoStatement(eventGotoStatement),
                BoundConditionalGotoStatement conditionalGotoStatement => RewriteConditionalGotoStatement(conditionalGotoStatement),
                BoundReturnStatement returnStatement => RewriteReturnStatement(returnStatement),
                BoundEmitterHintStatement emitterHintStatement => RewriteEmitterHint(emitterHintStatement),
                BoundCallStatement callStatement => RewriteCallStatement(callStatement),
                BoundExpressionStatement expressionStatement => RewriteExpressionStatement(expressionStatement),

                _ => throw new UnexpectedBoundNodeException(node),
            };
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

        protected virtual BoundStatement RewriteEventStatement(BoundEventStatement node)
        {
            BoundArgumentClause? argumentClause = node.ArgumentClause is null ? null : RewriteArgumentClause(node.ArgumentClause);

            BoundBlockStatement block = (BoundBlockStatement)RewriteBlockStatement(node.Block);

            if (argumentClause == node.ArgumentClause && block == node.Block)
                return node;

            return new BoundEventStatement(node.Syntax, node.Type, argumentClause, block);
        }

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node)
            => node;

        protected virtual BoundStatement RewritePostfixStatement(BoundPostfixStatement node)
            => node;

        protected virtual BoundStatement RewritePrefixStatement(BoundPrefixStatement node)
            => node;

        protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            if (node.OptionalAssignment is null)
                return node;

            BoundStatement assignment = RewriteStatement(node.OptionalAssignment);
            if (assignment == node.OptionalAssignment)
                return node;

            return new BoundVariableDeclarationStatement(node.Syntax, node.Variable, assignment);
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

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            BoundStatement body = RewriteStatement(node.Body);
            BoundExpression condition = RewriteExpression(node.Condition);
            if (body == node.Body && condition == node.Condition)
                return node;

            return new BoundDoWhileStatement(node.Syntax, body, condition, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
            => node;

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
            => node;

        protected virtual BoundStatement RewriteEventGotoStatement(BoundEventGotoStatement node)
        {
            if (node.ArgumentClause is null)
                return node;

            BoundArgumentClause clase = RewriteArgumentClause(node.ArgumentClause);

            if (clase == node.ArgumentClause)
                return node;

            return new BoundEventGotoStatement(
                node.Syntax,
                node.Label,
                node.EventType,
                clase
            );
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            BoundExpression? expression = node.Expression is null ? null : RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundReturnStatement(node.Syntax, expression);
        }

        protected virtual BoundStatement RewriteEmitterHint(BoundEmitterHintStatement node)
            => node;

        protected virtual BoundStatement RewriteCallStatement(BoundCallStatement node)
        {
            BoundArgumentClause argumentClause = RewriteArgumentClause(node.ArgumentClause);

            if (argumentClause == node.ArgumentClause)
                return node;

            return new BoundCallStatement(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType, node.ResultVariable);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(node.Syntax, expression);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            return node switch
            {
                BoundErrorExpression errorExpression => RewriteErrorExpression(errorExpression),
                BoundLiteralExpression literalExpression => RewriteLiteralExpression(literalExpression),
                BoundVariableExpression variableExpression => RewriteVariableExpression(variableExpression),
                BoundUnaryExpression unaryExpression => RewriteUnaryExpression(unaryExpression),
                BoundBinaryExpression binaryExpression => RewriteBinaryExpression(binaryExpression),
                BoundCallExpression callExpression => RewriteCallExpression(callExpression),
                BoundConversionExpression conversionExpression => RewriteConversionExpression(conversionExpression),
                BoundConstructorExpression constructorExpression => RewriteConstructorExpression(constructorExpression),
                BoundPostfixExpression postfixExpression => RewritePostfixExpression(postfixExpression),
                BoundPrefixExpression prefixExpression => RewritePrefixExpression(prefixExpression),
                BoundArraySegmentExpression arraySegmentExpression => RewriteArraySegmentExpression(arraySegmentExpression),
                BoundAssignmentExpression assignmentExpression => RewriteAssignmentExpression(assignmentExpression),
                BoundCompoundAssignmentExpression compoundAssignmentExpression => RewriteCompoundAssignmentExpression(compoundAssignmentExpression),

                _ => throw new UnexpectedBoundNodeException(node),
            };
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
            => node;

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
            => node;

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            switch (node.Variable)
            {
                case PropertySymbol prop:
                    {
                        BoundExpression newEx = RewriteExpression(prop.Expression);

                        if (newEx == prop.Expression)
                            return node;

                        return new BoundVariableExpression(node.Syntax, new PropertySymbol(prop.Definition, newEx));
                    }
                default:
                    return node;
            }
        }

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

        protected virtual BoundExpression RewritePostfixExpression(BoundPostfixExpression node)
            => node;

        protected virtual BoundExpression RewritePrefixExpression(BoundPrefixExpression node)
            => node;

        protected virtual BoundExpression RewriteArraySegmentExpression(BoundArraySegmentExpression node)
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

            return new BoundArraySegmentExpression(node.Syntax, node.ElementType, builder.ToImmutable());
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Syntax, node.Variable, expression);
        }

        protected virtual BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundCompoundAssignmentExpression(node.Syntax, node.Variable, node.Op, expression);
        }

        #region Helper functions
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
        #endregion
    }
}
