using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    // sadly cannot use BoundTreeRewriter, because expressions need to return more data
    // required for stuff like assignments, ++, -- and non void functions, because these can only be executed from a void (execution) wire, so they are "extracted" and ran before/after the expression
    internal sealed class StatementExpressionExtractor
    {
        private State state = new State();

        public static BoundStatement Extract(BoundStatement statement)
        {
            StatementExpressionExtractor extractor = new StatementExpressionExtractor();
            return extractor.RewriteStatement(statement);
        }

        private BoundStatement RewriteStatement(BoundStatement node)
        {
            state.ResetVarCount();

            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)node);
                case BoundNodeKind.EventStatement:
                    return RewriteEventStatement((BoundEventStatement)node);
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement)node);
                case BoundNodeKind.PostfixStatement:
                    return RewritePostfixStatement((BoundPostfixStatement)node);
                case BoundNodeKind.PrefixStatement:
                    return RewritePrefixStatement((BoundPrefixStatement)node);
                case BoundNodeKind.VariableDeclarationStatement:
                    return RewriteVariableDeclaration((BoundVariableDeclarationStatement)node);
                case BoundNodeKind.AssignmentStatement:
                    return RewriteAssignmentStatement((BoundAssignmentStatement)node);
                case BoundNodeKind.CompoundAssignmentStatement:
                    return RewriteCompoundAssignmentStatement((BoundCompoundAssignmentStatement)node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)node);
                //case BoundNodeKind.WhileStatement:
                //    return RewriteWhileStatement((BoundWhileStatement)node);
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
                    return RewriteReturnStatement((BoundReturnStatement)node);
                case BoundNodeKind.EmitterHint:
                    return RewriteEmitterHint((BoundEmitterHint)node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        private BoundStatement RewriteBlockStatement(BoundBlockStatement node)
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

        private BoundStatement RewriteEventStatement(BoundEventStatement node)
        {
            var (before, argumentClause, after) = node.ArgumentClause is null ? (ImmutableArray<BoundStatement>.Empty, null, ImmutableArray<BoundStatement>.Empty) : RewriteArgumentClause(node.ArgumentClause);

            BoundBlockStatement block = (BoundBlockStatement)RewriteBlockStatement(node.Block);

            if (argumentClause == node.ArgumentClause && block == node.Block && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)
                return node;

            return HandleBeforeAfter(before, new BoundEventStatement(node.Syntax, node.Type, argumentClause, block), after);
        }

        private BoundStatement RewriteNopStatement(BoundNopStatement node)
            => node;

        private BoundStatement RewritePostfixStatement(BoundPostfixStatement node)
            => node;

        private BoundStatement RewritePrefixStatement(BoundPrefixStatement node)
            => node;

        private BoundStatement RewriteVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            if (node.OptionalAssignment is null)
                return node;

            BoundStatement assignment = RewriteStatement(node.OptionalAssignment);
            if (assignment == node.OptionalAssignment)
                return node;

            return new BoundVariableDeclarationStatement(node.Syntax, node.Variable, assignment);
        }

        private BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            ExpressionResult expression = RewriteExpression(node.Expression);
            if (expression.IsSameAs(node.Expression))
                return node;

            return HandleBeforeAfter(expression.Before, new BoundAssignmentStatement(node.Syntax, node.Variable, expression.Expression), expression.After);
        }

        private BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
        {
            ExpressionResult expression = RewriteExpression(node.Expression);
            if (expression.IsSameAs(node.Expression))
                return node;

            return HandleBeforeAfter(expression.Before, new BoundCompoundAssignmentStatement(node.Syntax, node.Variable, node.Op, expression.Expression), expression.After);
        }

        private BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            ExpressionResult condition = RewriteExpression(node.Condition);
            BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = node.ElseStatement is null ? null : RewriteStatement(node.ElseStatement);
            if (condition.IsSameAs(node.Condition) && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
                return node;

            if (!condition.Any)
                return new BoundIfStatement(node.Syntax, condition.Expression, thenStatement, elseStatement);

            return HandleBeforeAfterWithTemp(condition, newEx => new BoundIfStatement(node.Syntax, newEx, thenStatement, elseStatement));
        }

        //private BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        //{
        //    ExpressionResult condition = RewriteExpression(node.Condition);
        //    BoundStatement body = RewriteStatement(node.Body);
        //    if (condition.IsSameAs(node.Condition) && body == node.Body)
        //        return node;

        //    if (!condition.Any)
        //        return new BoundWhileStatement(node.Syntax, condition.Expression, body, node.BreakLabel, node.ContinueLabel);

        //    // TODO: how?? both before and after need to be run every iteration, or just.. don't support these, this should be ran after lowerer anyways...

        //    return new BoundWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        //}

        //private BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        //{
        //    BoundStatement body = RewriteStatement(node.Body);
        //    ExpressionResult condition = RewriteExpression(node.Condition);
        //    if (body == node.Body && condition.IsSameAs(node.Condition))
        //        return node;

        //    if (!condition.Any)
        //        return new BoundDoWhileStatement(node.Syntax, body, condition.Expression, node.BreakLabel, node.ContinueLabel);

        //    // TODO: how?? both before and after need to be run every iteration, or just.. don't support these, this should be ran after lowerer anyways...

        //    return new BoundDoWhileStatement(node.Syntax, body, condition, node.BreakLabel, node.ContinueLabel);
        //}

        private BoundStatement RewriteLabelStatement(BoundLabelStatement node)
            => node;

        private BoundStatement RewriteGotoStatement(BoundGotoStatement node)
            => node;

        private BoundStatement RewriteRollbackGotoStatement(BoundRollbackGotoStatement node)
            => node;

        private BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition is BoundEventCondition)
                return node; // TODO:

            ExpressionResult condition = RewriteExpression(node.Condition);
            if (condition.IsSameAs(node.Condition))
                return node;

            if (!condition.Any)
                return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition.Expression, node.JumpIfTrue);

            return HandleBeforeAfterWithTemp(condition, newEx => new BoundConditionalGotoStatement(node.Syntax, node.Label, newEx, node.JumpIfTrue));
        }

        private BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            if (node.Expression is null)
                return node;

            ExpressionResult expression = RewriteExpression(node.Expression);
            if (expression.IsSameAs(node.Expression))
                return node;

            if (!expression.Any)
                return new BoundReturnStatement(node.Syntax, expression.Expression);

            return HandleBeforeAfterWithTemp(expression, newEx => new BoundReturnStatement(node.Syntax, newEx));
        }

        private BoundStatement RewriteEmitterHint(BoundEmitterHint node)
            => node;

        private BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            ExpressionResult expression = RewriteExpression(node.Expression);
            if (expression.IsSameAs(node.Expression))
                return node;

            return HandleBeforeAfter(expression.Before, new BoundExpressionStatement(node.Syntax, expression.Expression), expression.After);
        }

        private ExpressionResult RewriteExpression(BoundExpression node)
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
                case BoundNodeKind.PostfixExpression:
                    return RewritePostfixExpression((BoundPostfixExpression)node);
                case BoundNodeKind.PrefixExpression:
                    return RewritePrefixExpression((BoundPrefixExpression)node);
                case BoundNodeKind.ArraySegmentExpression:
                    return RewriteArraySegmentExpression((BoundArraySegmentExpression)node);
                case BoundNodeKind.StatementExpression:
                    return RewriteStatementExpression((BoundStatementExpression)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        private ExpressionResult RewriteErrorExpression(BoundErrorExpression node)
            => new ExpressionResult(node);

        private ExpressionResult RewriteLiteralExpression(BoundLiteralExpression node)
            => new ExpressionResult(node);

        private ExpressionResult RewriteVariableExpression(BoundVariableExpression node)
        {
            switch (node.Variable)
            {
                case PropertySymbol prop:
                    {
                        ExpressionResult newEx = RewriteExpression(prop.Expression);

                        if (newEx.IsSameAs(prop.Expression))
                            return new ExpressionResult(node);

                        return ExpressionResult.Enclose(new BoundVariableExpression(node.Syntax, new PropertySymbol(prop.Definition, newEx.Expression)), newEx);
                    }
                default:
                    return new ExpressionResult(node);
            }
        }

        private ExpressionResult RewriteUnaryExpression(BoundUnaryExpression node)
        {
            ExpressionResult operand = RewriteExpression(node.Operand);

            if (operand.IsSameAs(node.Operand))
                return new ExpressionResult(node);

            return ExpressionResult.Enclose(new BoundUnaryExpression(node.Syntax, node.Op, operand.Expression), operand);
        }

        private ExpressionResult RewriteBinaryExpression(BoundBinaryExpression node)
        {
            ExpressionResult left = RewriteExpression(node.Left);
            ExpressionResult right = RewriteExpression(node.Right);

            if (left.IsSameAs(node.Left) && right.IsSameAs(node.Right))
                return new ExpressionResult(node);

            // TODO: special case for || and && - short circuting if (a || b) - if a is true, b doesn't get ran, neighter do b's before and after

            var (before, after, leftEx, rightEx) = ExpressionResult.Resolve(state, left, right);

            return new ExpressionResult(before, new BoundBinaryExpression(node.Syntax, leftEx, node.Op, rightEx), after);
        }

        private ExpressionResult RewriteCallExpression(BoundCallExpression node)
        {
            var (before, argumentClause, after) = RewriteArgumentClause(node.ArgumentClause);

            if (argumentClause == node.ArgumentClause && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)
                return new ExpressionResult(node);

            return new ExpressionResult(before, new BoundCallExpression(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType), after);
        }

        private ExpressionResult RewriteConversionExpression(BoundConversionExpression node)
        {
            ExpressionResult expression = RewriteExpression(node.Expression);

            if (expression.IsSameAs(node.Expression))
                return new ExpressionResult(node);

            return ExpressionResult.Enclose(new BoundConversionExpression(node.Syntax, node.Type, expression.Expression), expression);
        }

        private ExpressionResult RewriteConstructorExpression(BoundConstructorExpression node)
        {
            ExpressionResult exX = RewriteExpression(node.ExpressionX);
            ExpressionResult exY = RewriteExpression(node.ExpressionY);
            ExpressionResult exZ = RewriteExpression(node.ExpressionZ);

            if (exX.IsSameAs(node.ExpressionX) && exY.IsSameAs(node.ExpressionY) && exZ.IsSameAs(node.ExpressionZ))
                return new ExpressionResult(node);

            var (before, after, expressions) = ExpressionResult.Resolve(state, [exX, exY, exZ]);

            return new ExpressionResult(before, new BoundConstructorExpression(node.Syntax, node.Type, expressions[0], expressions[1], expressions[2]), after);
        }

        private ExpressionResult RewritePostfixExpression(BoundPostfixExpression node)
        {
            return new ExpressionResult([], Variable(node.Syntax, node.Variable), [PostfixStatement(node.Syntax, node.Variable, node.PostfixKind)]);
        }

        private ExpressionResult RewritePrefixExpression(BoundPrefixExpression node)
        {
            return new ExpressionResult([PrefixStatement(node.Syntax, node.Variable, node.PrefixKind)], Variable(node.Syntax, node.Variable), []);
        }

        private ExpressionResult RewriteArraySegmentExpression(BoundArraySegmentExpression node)
        {
            ImmutableArray<ExpressionResult>.Builder? builder = null;

            for (var i = 0; i < node.Elements.Length; i++)
            {
                BoundExpression oldElement = node.Elements[i];
                ExpressionResult newElement = RewriteExpression(oldElement);
                if (!newElement.IsSameAs(oldElement))
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<ExpressionResult>(node.Elements.Length);

                        for (int j = 0; j < i; j++)
                            builder.Add(new ExpressionResult(node.Elements[j]));
                    }
                }

                if (builder is not null)
                    builder.Add(newElement);
            }

            if (builder is null)
                return new ExpressionResult(node);

            var (before, after, expressions) = ExpressionResult.Resolve(state, builder.ToImmutable().AsSpan());

            return new ExpressionResult(before, new BoundArraySegmentExpression(node.Syntax, node.ElementType, expressions.ToImmutableArray()), after);
        }

        private ExpressionResult RewriteStatementExpression(BoundStatementExpression node)
        {
            BoundStatement statement = RewriteStatement(node.Statement);

            return new ExpressionResult([statement], new BoundNopExpression(node.Syntax), []);
        }

        #region Helper functions
        private (ImmutableArray<BoundStatement> Before, BoundArgumentClause Clause, ImmutableArray<BoundStatement> After) RewriteArgumentClause(BoundArgumentClause node)
        {
            ImmutableArray<ExpressionResult>.Builder? builder = null;

            for (var i = 0; i < node.Arguments.Length; i++)
            {
                BoundExpression oldArgument = node.Arguments[i];
                ExpressionResult newArgument = RewriteExpression(oldArgument);
                if (!newArgument.IsSameAs(oldArgument))
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<ExpressionResult>(node.Arguments.Length);

                        for (int j = 0; j < i; j++)
                            builder.Add(new ExpressionResult(node.Arguments[j]));
                    }
                }

                if (builder is not null)
                    builder.Add(newArgument);
            }

            if (builder is null)
                return ([], node, []);

            var (before, after, expressions) = ExpressionResult.Resolve(state, builder.ToImmutable().AsSpan());

            return (before, new BoundArgumentClause(node.Syntax, node.ArgModifiers, expressions.ToImmutableArray()), after);
        }

        private BoundStatement HandleBeforeAfterWithTemp(ExpressionResult result, Func<BoundExpression, BoundStatement> getStatement)
            => HandleBeforeAfterWithTemp(result.Before, result.Expression, result.After, getStatement);
        private BoundStatement HandleBeforeAfterWithTemp(ImmutableArray<BoundStatement> before, BoundExpression expression, ImmutableArray<BoundStatement> after, Func<BoundExpression, BoundStatement> getStatement)
        {
            if (after.IsDefaultOrEmpty)
                return HandleBeforeAfter(before, getStatement(expression), after); // don't need to worry about after, temp var, because no changes are made after the expression

            VariableSymbol temp = state.GetTempVar(expression.Type);

            BoundStatement statement = HandleBeforeAfter(before, Assignment(expression.Syntax, temp, expression), after);

            return Block(
                statement.Syntax,
                statement,
                getStatement(Variable(expression.Syntax, temp))
            );
        }
        private BoundStatement HandleBeforeAfter(ImmutableArray<BoundStatement> before, BoundStatement statement, ImmutableArray<BoundStatement> after)
        {
            if (before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)
                return statement;
            else if (before.IsDefaultOrEmpty)
            {
                BoundStatement[] statements = new BoundStatement[after.Length + 1];
                statements[0] = statement;
                after.CopyTo(statements, 1);
                return Block(statement.Syntax, statements);
            }
            else if (after.IsDefaultOrEmpty)
            {
                BoundStatement[] statements = new BoundStatement[before.Length + 1];
                before.CopyTo(statements, 0);
                statements[^1] = statement;
                return Block(statement.Syntax, statements);
            }
            else
            {
                BoundStatement[] statements = new BoundStatement[before.Length + after.Length + 1];
                before.CopyTo(statements, 0);
                statements[before.Length] = statement;
                after.CopyTo(statements, before.Length + 1);
                return Block(statement.Syntax, statements);
            }
        }
        #endregion

        private class State
        {
            private Counter varCounter = new Counter(0);

            public VariableSymbol GetTempVar(TypeSymbol type)
            {
                VariableSymbol var = new ReservedCompilerVariableSymbol("temp", varCounter.ToString(), Modifiers.Readonly, type);
                varCounter++;
                return var;
            }

            public void ResetVarCount()
            {
                varCounter = new Counter(0);
            }
        }

        private readonly struct ExpressionResult
        {
            public bool Any => !Before.IsDefaultOrEmpty || !After.IsDefaultOrEmpty;

            /// <summary>
            /// Statements to be executed before <see cref="Expression"/> is evaluated.
            /// </summary>
            public readonly ImmutableArray<BoundStatement> Before;
            public readonly BoundExpression Expression;
            /// <summary>
            /// Statements to be executed after <see cref="Expression"/> is evaluated.
            /// </summary>
            public readonly ImmutableArray<BoundStatement> After;

            public ExpressionResult(BoundExpression expression)
            {
                Expression = expression;
            }

            public ExpressionResult(ImmutableArray<BoundStatement> before, BoundExpression expression, ImmutableArray<BoundStatement> after)
            {
                Before = before;
                Expression = expression;
                After = after;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ExpressionResult Enclose(BoundExpression expression, ExpressionResult result)
                => new ExpressionResult(result.Before, expression, result.After);
            public static (ImmutableArray<BoundStatement> Before, ImmutableArray<BoundStatement> After, BoundExpression Ex1, BoundExpression Ex2) Resolve(State state, ExpressionResult res1, ExpressionResult res2)
            {
                if (res1.After.IsDefaultOrEmpty && res2.Before.IsDefaultOrEmpty)
                    return (res1.Before, res2.After, res1.Expression, res2.Expression);

                List<BoundStatement> before = new List<BoundStatement>();
                ImmutableArray<BoundStatement> after = default;

                addBefore(res1.Before);

                VariableSymbol res1Temp = state.GetTempVar(res1.Expression.Type);
                before.Add(Assignment(res1.Expression.Syntax, res1Temp, res1.Expression));

                addBefore(res1.After);
                addBefore(res2.Before);

                after = res2.After;

                return
                (
                    before.ToImmutableArray(),
                    after,
                    Variable(res1.Expression.Syntax, res1Temp),
                    res2.Expression
                );

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void addBefore(ImmutableArray<BoundStatement> arr)
                {
                    if (!arr.IsDefaultOrEmpty)
                        before.AddRange(arr);
                }
            }

            public static (ImmutableArray<BoundStatement> Before, ImmutableArray<BoundStatement> After, BoundExpression[] Expressions) Resolve(State state, ReadOnlySpan<ExpressionResult> results)
            {
                switch (results.Length)
                {
                    case 0:
                        return (ImmutableArray<BoundStatement>.Empty, ImmutableArray<BoundStatement>.Empty, []);
                    case 1:
                        return (results[0].Before, results[0].After, [results[0].Expression]);
                    case 2:
                        {
                            var (b, a, ex1, ex2) = Resolve(state, results[0], results[1]);
                            return (b, a, [ex1, ex2]);
                        }
                }

                // optimize for when only the first has before (or not) and the last has after (or not)
                bool found = false;
                for (int i = 0; i < results.Length; i++)
                {
                    if (i == 0)
                    {
                        if (!results[i].After.IsDefaultOrEmpty)
                        {
                            found = true;
                            break;
                        }
                    }
                    else if (i == results.Length - 1)
                    {
                        if (!results[i].Before.IsDefaultOrEmpty)
                        {
                            found = true;
                            break;
                        }
                    }
                    else if (!results[i].Before.IsDefaultOrEmpty || !results[i].After.IsDefaultOrEmpty)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    BoundExpression[] expresions = new BoundExpression[results.Length];
                    for (int i = 0; i < results.Length; i++)
                        expresions[i] = results[i].Expression;

                    return (results[0].Before, results[^1].After, expresions);
                }

                List<BoundStatement> before = new List<BoundStatement>();
                ImmutableArray<BoundStatement> after = default;
                BoundExpression[] expressions = new BoundExpression[results.Length];

                for (int i = 0; i < results.Length; i++)
                {
                    ref readonly ExpressionResult res = ref results[i];

                    add(res.Before);

                    // TODO: this shouldn't be for the last result, but for the last result that has any and for the results after
                    if (i == results.Length - 1)
                    {
                        expressions[i] = res.Expression;
                        after = res.After;
                    }
                    else
                    {
                        VariableSymbol temp = state.GetTempVar(res.Expression.Type);
                        before.Add(Assignment(res.Expression.Syntax, temp, res.Expression));
                        add(res.After);
                        expressions[i] = Variable(res.Expression.Syntax, temp);
                    }
                }

                return
                (
                    before.ToImmutableArray(),
                    after,
                    expressions
                );

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void add(ImmutableArray<BoundStatement> arr)
                {
                    if (!arr.IsDefaultOrEmpty)
                        before.AddRange(arr);
                }
            }

            public bool IsSameAs(BoundExpression ex)
            {
                if (!Before.IsDefaultOrEmpty || !After.IsDefaultOrEmpty)
                    return false;
                else
                    return Expression == ex;
            }

            private static bool canUseInline(BoundExpression expression)
            {
                return false; // TODO
            }
        }
    }
}
