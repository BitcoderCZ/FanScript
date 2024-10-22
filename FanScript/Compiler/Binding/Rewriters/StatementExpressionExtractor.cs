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
        private State state;

        public StatementExpressionExtractor(FunctionSymbol function)
        {
            state = new State(function);
        }

        public static BoundStatement Extract(FunctionSymbol function, BoundStatement statement)
        {
            StatementExpressionExtractor extractor = new StatementExpressionExtractor(function);
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
                case BoundNodeKind.CallStatement:
                    return RewriteCallStatement((BoundCallStatement)node);
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

            return new BoundBlockStatement(node.Syntax, builder.DrainToImmutable());
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
            if (node.Condition is BoundEventCondition eventCondition)
            {
                if (eventCondition.ArgumentClause is null)
                    return node;

                var (before, clause) = RewriteArgumentClauseBeforeOnly(eventCondition.ArgumentClause);

                if (clause == eventCondition.ArgumentClause)
                    return node;

                return HandleBeforeAfter(
                    before,
                    new BoundConditionalGotoStatement(node.Syntax, node.Label,
                        new BoundEventCondition(
                            eventCondition.Syntax,
                            eventCondition.EventType,
                            clause
                        ), node.JumpIfTrue),
                    ImmutableArray<BoundStatement>.Empty
                );
            }

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

        private BoundStatement RewriteCallStatement(BoundCallStatement node)
        {
            var (before, clause, after) = RewriteArgumentClause(node.ArgumentClause);

            if (clause == node.ArgumentClause && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)
                return node;

            return HandleBeforeAfter(before, new BoundCallStatement(node.Syntax, node.Function, clause, node.ReturnType, node.GenericType, node.ResultVariable), after);
        }

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
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression)node);
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

            var opKind = node.Op.Kind;
            if (right.Any && (opKind == BoundBinaryOperatorKind.LogicalAnd || opKind == BoundBinaryOperatorKind.LogicalOr))
            {
                // Short-circuit evaluation
                var variable = state.GetTempVar(left.Expression.Type);
                var varEx = Variable(left.Expression.Syntax, variable);

                var before = ImmutableArray.CreateBuilder<BoundStatement>(left.Length + 1 + right.Length);
                ImmutableArray<BoundStatement> after = right.After;

                before.AddRangeSafe(left.Before);
                before.Add(Assignment(left.Expression.Syntax, variable, left.Expression));
                before.AddRangeSafe(left.After);

                BoundLabel label = state.GetLabel("short_circuit");

                if (opKind == BoundBinaryOperatorKind.LogicalAnd)
                    before.Add(GotoFalse(left.Expression.Syntax, label, varEx));
                else
                    before.Add(GotoTrue(left.Expression.Syntax, label, varEx));

                before.AddRangeSafe(right.Before);
                before.Add(Assignment(right.Expression.Syntax, variable, right.Expression));
                before.AddRangeSafe(right.After);
                before.Add(Label(right.Expression.Syntax, label));

                return new ExpressionResult(before.DrainToImmutable(), varEx, after);
            }
            else
            {
                var (before, after, leftEx, rightEx) = ExpressionResult.Resolve(state, left, right);

                return new ExpressionResult(before, new BoundBinaryExpression(node.Syntax, leftEx, node.Op, rightEx), after);
            }
        }

        private ExpressionResult RewriteCallExpression(BoundCallExpression node)
        {
            var (before, argumentClause, after) = RewriteArgumentClause(node.ArgumentClause);

            bool extract = node.Function.Type != TypeSymbol.Void && node.Function is not BuiltinFunctionSymbol;

            if (argumentClause == node.ArgumentClause && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty &&
                !extract)
                return new ExpressionResult(node);

            if (extract)
            {
                var temp = state.GetTempVar(node.Function.Type, true);

                return new ExpressionResult(
                    (before.IsDefault ? Enumerable.Empty<BoundStatement>() : before).Concat([
                        new BoundCallStatement(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType, temp)
                    ]).ToImmutableArray(),
                    Variable(node.Syntax, temp),
                    after
                );
            }
            else
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

            var (before, after, expressions) = ExpressionResult.Resolve(state, builder.DrainToImmutable().AsSpan());

            return new ExpressionResult(before, new BoundArraySegmentExpression(node.Syntax, node.ElementType, expressions.ToImmutableArray()), after);
        }

        private ExpressionResult RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            ExpressionResult expression = RewriteExpression(node.Expression);

            return new ExpressionResult(
                (expression.Before.IsDefault ? Enumerable.Empty<BoundStatement>() : expression.Before).Concat([
                    Assignment(node.Syntax, node.Variable, expression.Expression)
                ]).ToImmutableArray(),
                Variable(node.Syntax, node.Variable),
                expression.After
            );
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

            var (before, after, expressions) = ExpressionResult.Resolve(state, builder.DrainToImmutable().AsSpan());

            return (before, new BoundArgumentClause(node.Syntax, node.ArgModifiers, expressions.ToImmutableArray()), after);
        }
        private (ImmutableArray<BoundStatement> Before, BoundArgumentClause Clause) RewriteArgumentClauseBeforeOnly(BoundArgumentClause node)
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
                return ([], node);

            var (before, expressions) = ExpressionResult.ResolveBeforeOnly(state, builder.DrainToImmutable().AsSpan());

            return (before, new BoundArgumentClause(node.Syntax, node.ArgModifiers, expressions.ToImmutableArray()));
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
            private FunctionSymbol function;

            public State(FunctionSymbol function)
            {
                this.function = function;
            }

            private Counter varCounter = new Counter(0);
            private Counter labelCounter = new Counter(0);

            public VariableSymbol GetTempVar(TypeSymbol type, bool inline = false)
            {
                VariableSymbol var = new ReservedCompilerVariableSymbol("temp", varCounter.ToString(), inline ? Modifiers.Inline : 0, type);
                varCounter++;
                return var;
            }

            public BoundLabel GetLabel(string name)
            {
                return new BoundLabel(function.Name + "_" + name + labelCounter++);
            }

            public void ResetVarCount()
            {
                varCounter = new Counter(0);
            }
        }

        private readonly struct ExpressionResult
        {
            public bool Any => !Before.IsDefaultOrEmpty || !After.IsDefaultOrEmpty;
            public int Length => Before.LengthOrZero() + 1 + After.LengthOrZero();

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
                    else if (results[i].Any)
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

                int lastAnyIndex = -1;
                bool foundAfter = false;
                for (int i = results.Length - 1; i >= 0; i--)
                {
                    ref readonly ExpressionResult res = ref results[i];

                    if (!res.After.IsDefaultOrEmpty)
                    {
                        if (foundAfter)
                        {
                            lastAnyIndex = i + 1;
                            break;
                        }
                        else
                            foundAfter = true;
                    }

                    if (!res.Before.IsDefaultOrEmpty)
                    {
                        lastAnyIndex = i;
                        break;
                    }
                }

                VariableSymbol? lastTemp = null;
                for (int i = 0; i < results.Length; i++)
                {
                    ref readonly ExpressionResult res = ref results[i];

                    add(res.Before);

                    if (i >= lastAnyIndex)
                    {
                        expressions[i] = res.Expression;
                        if (!res.After.IsDefaultOrEmpty)
                            after = res.After;
                    }
                    else
                    {
                        VariableSymbol temp;
                        if (lastTemp is not null && res.Before.IsDefaultOrEmpty)
                            temp = lastTemp;
                        else
                        {
                            temp = state.GetTempVar(res.Expression.Type);
                            before.Add(Assignment(res.Expression.Syntax, temp, res.Expression));
                        }

                        add(res.After);
                        expressions[i] = Variable(res.Expression.Syntax, temp);

                        if (res.After.IsDefaultOrEmpty)
                            lastTemp = temp;
                        else
                            lastTemp = null;
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

            public static (ImmutableArray<BoundStatement> Before, BoundExpression[] Expressions) ResolveBeforeOnly(State state, ReadOnlySpan<ExpressionResult> results)
            {
                switch (results.Length)
                {
                    case 0:
                        return (ImmutableArray<BoundStatement>.Empty, []);
                    case 1:
                        {
                            var res = results[0];
                            if (res.After.IsDefaultOrEmpty)
                                return (res.Before, [res.Expression]);
                            else
                            {
                                int beforeLen = res.Before.IsDefault ? 0 : res.Before.Length;

                                var builder = ImmutableArray.CreateBuilder<BoundStatement>(beforeLen + res.After.Length + 1);

                                builder.AddRange(res.Before, beforeLen);

                                var tempVar = state.GetTempVar(res.Expression.Type);

                                builder.Add(Assignment(res.Expression.Syntax, tempVar, res.Expression));

                                builder.AddRange(res.After);

                                return (builder.DrainToImmutable(), [Variable(res.Expression.Syntax, tempVar)]);
                            }
                        }
                }

                // optimize for when only the first has before (or not)
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
                    else if (results[i].Any)
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

                    return (results[0].Before, expresions);
                }

                List<BoundStatement> before = new List<BoundStatement>();
                BoundExpression[] expressions = new BoundExpression[results.Length];

                int lastAnyIndex = -1;
                for (int i = results.Length - 1; i >= 0; i--)
                {
                    if (results[i].Any)
                    {
                        lastAnyIndex = i;
                        break;
                    }
                }

                VariableSymbol? lastTemp = null;
                for (int i = 0; i < results.Length; i++)
                {
                    ref readonly ExpressionResult res = ref results[i];

                    add(res.Before);

                    if (i > lastAnyIndex)
                        expressions[i] = res.Expression;
                    else
                    {
                        VariableSymbol temp;
                        if (lastTemp is not null && res.Before.IsDefaultOrEmpty)
                            temp = lastTemp;
                        else
                        {
                            temp = state.GetTempVar(res.Expression.Type);
                            before.Add(Assignment(res.Expression.Syntax, temp, res.Expression));
                        }

                        add(res.After);
                        expressions[i] = Variable(res.Expression.Syntax, temp);

                        if (res.After.IsDefaultOrEmpty)
                            lastTemp = temp;
                        else
                            lastTemp = null;
                    }
                }

                return
                (
                    before.ToImmutableArray(),
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
