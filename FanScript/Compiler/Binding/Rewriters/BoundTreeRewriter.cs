using System.Collections.Immutable;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols.Variables;

namespace FanScript.Compiler.Binding.Rewriters
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
            => node switch
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

                // case BoundNodeKind.ForStatement:
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

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;

            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement oldStatement = node.Statements[i];
                BoundStatement newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);

                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(node.Statements[j]);
                        }
                    }
                }

                builder?.Add(newStatement);
            }

            return builder is null ? node : (BoundStatement)new BoundBlockStatement(node.Syntax, builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteEventStatement(BoundEventStatement node)
        {
            BoundArgumentClause? argumentClause = node.ArgumentClause is null ? null : RewriteArgumentClause(node.ArgumentClause);

            BoundBlockStatement block = (BoundBlockStatement)RewriteBlockStatement(node.Block);

            return argumentClause == node.ArgumentClause && block == node.Block
                ? node
                : (BoundStatement)new BoundEventStatement(node.Syntax, node.Type, argumentClause, block);
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
            {
                return node;
            }

            BoundStatement assignment = RewriteStatement(node.OptionalAssignment);
            return assignment == node.OptionalAssignment ? node : new BoundVariableDeclarationStatement(node.Syntax, node.Variable, assignment);
        }

        protected virtual BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundAssignmentStatement(node.Syntax, node.Variable, expression);
        }

        protected virtual BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundCompoundAssignmentStatement(node.Syntax, node.Variable, node.Op, expression);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = node.ElseStatement is null ? null : RewriteStatement(node.ElseStatement);
            return condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement
                ? node
                : new BoundIfStatement(node.Syntax, condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);
            return condition == node.Condition && body == node.Body
                ? node
                : new BoundWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            BoundStatement body = RewriteStatement(node.Body);
            BoundExpression condition = RewriteExpression(node.Condition);
            return body == node.Body && condition == node.Condition
                ? node
                : new BoundDoWhileStatement(node.Syntax, body, condition, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
            => node;

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
            => node;

        protected virtual BoundStatement RewriteEventGotoStatement(BoundEventGotoStatement node)
        {
            if (node.ArgumentClause is null)
            {
                return node;
            }

            BoundArgumentClause clase = RewriteArgumentClause(node.ArgumentClause);

            return clase == node.ArgumentClause
                ? node
                : (BoundStatement)new BoundEventGotoStatement(
                node.Syntax,
                node.Label,
                node.EventType,
                clase);
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            return condition == node.Condition ? node : new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            BoundExpression? expression = node.Expression is null ? null : RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundReturnStatement(node.Syntax, expression);
        }

        protected virtual BoundStatement RewriteEmitterHint(BoundEmitterHintStatement node)
            => node;

        protected virtual BoundStatement RewriteCallStatement(BoundCallStatement node)
        {
            BoundArgumentClause argumentClause = RewriteArgumentClause(node.ArgumentClause);

            return argumentClause == node.ArgumentClause
                ? node
                : new BoundCallStatement(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType, node.ResultVariable);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundExpressionStatement(node.Syntax, expression);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
        public virtual BoundExpression RewriteExpression(BoundExpression node)
            => node switch
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

                        return newEx == prop.Expression ? node : new BoundVariableExpression(node.Syntax, new PropertySymbol(prop.Definition, newEx));
                    }

                default:
                    return node;
            }
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression operand = RewriteExpression(node.Operand);

            return operand == node.Operand ? node : new BoundUnaryExpression(node.Syntax, node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression left = RewriteExpression(node.Left);
            BoundExpression right = RewriteExpression(node.Right);

            return left == node.Left && right == node.Right ? node : new BoundBinaryExpression(node.Syntax, left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            BoundArgumentClause argumentClause = RewriteArgumentClause(node.ArgumentClause);

            return argumentClause == node.ArgumentClause
                ? node
                : new BoundCallExpression(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType);
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);

            return expression == node.Expression ? node : new BoundConversionExpression(node.Syntax, node.Type, expression);
        }

        protected virtual BoundExpression RewriteConstructorExpression(BoundConstructorExpression node)
        {
            BoundExpression expressionX = RewriteExpression(node.ExpressionX);
            BoundExpression expressionY = RewriteExpression(node.ExpressionY);
            BoundExpression expressionZ = RewriteExpression(node.ExpressionZ);

            bool xDiff = expressionX != node.ExpressionX,
                yDiff = expressionY != node.ExpressionY,
                zDiff = expressionZ != node.ExpressionZ;

            return xDiff || yDiff || zDiff
                ? new BoundConstructorExpression(
                    node.Syntax,
                    node.Type,
                    xDiff ? expressionX : node.ExpressionX,
                    yDiff ? expressionY : node.ExpressionY,
                    zDiff ? expressionZ : node.ExpressionZ)
                : (BoundExpression)node;
        }

        protected virtual BoundExpression RewritePostfixExpression(BoundPostfixExpression node)
            => node;

        protected virtual BoundExpression RewritePrefixExpression(BoundPrefixExpression node)
            => node;

        protected virtual BoundExpression RewriteArraySegmentExpression(BoundArraySegmentExpression node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (int i = 0; i < node.Elements.Length; i++)
            {
                BoundExpression oldElement = node.Elements[i];
                BoundExpression newElement = RewriteExpression(oldElement);
                if (newElement != oldElement)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Elements.Length);

                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(node.Elements[j]);
                        }
                    }
                }

                builder?.Add(newElement);
            }

            return builder is null ? node : new BoundArraySegmentExpression(node.Syntax, node.ElementType, builder.ToImmutable());
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundAssignmentExpression(node.Syntax, node.Variable, expression);
        }

        protected virtual BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            return expression == node.Expression ? node : new BoundCompoundAssignmentExpression(node.Syntax, node.Variable, node.Op, expression);
        }

        #region Helper functions
        protected virtual BoundArgumentClause RewriteArgumentClause(BoundArgumentClause node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (int i = 0; i < node.Arguments.Length; i++)
            {
                BoundExpression oldArgument = node.Arguments[i];
                BoundExpression newArgument = RewriteExpression(oldArgument);
                if (newArgument != oldArgument)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);

                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(node.Arguments[j]);
                        }
                    }
                }

                builder?.Add(newArgument);
            }

            return builder is null ? node : new BoundArgumentClause(node.Syntax, node.ArgModifiers, builder.ToImmutable());
        }
        #endregion
    }
}
