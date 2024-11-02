using System.Collections.Immutable;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    internal sealed class Inliner : BoundTreeRewriter
    {
        private readonly BoundAnalysisResult _analysisResult;
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> _functions;

        private Counter _varCount = new Counter(0);
        private Counter _labelCount = new Counter(0);

        public Inliner(BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, Continuation? continuation = null)
        {
            _analysisResult = analysisResult;
            _functions = functions;

            if (continuation is not null)
            {
                _varCount = continuation.Value.LastCount;
                _labelCount = continuation.Value.LastLabelCount;
            }
        }

        public static BoundBlockStatement Inline(BoundBlockStatement statement, BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref Continuation? continuation)
        {
            Inliner inliner = new Inliner(analysisResult, functions, continuation);
            BoundStatement res = inliner.RewriteBlockStatement(statement);

            continuation = new Continuation(inliner._varCount, inliner._labelCount);

            return res is BoundBlockStatement blockRes ? blockRes : new BoundBlockStatement(res.Syntax, [res]);
        }

        protected override BoundStatement RewriteCallStatement(BoundCallStatement node)
            => _analysisResult.ShouldFunctionGetInlined(node.Function)
                ? CallInliner.Inline(node, this, node.ResultVariable is not null, ref _varCount, ref _labelCount)
                : base.RewriteCallStatement(node);

        public readonly struct Continuation
        {
            public readonly Counter LastCount;
            public readonly Counter LastLabelCount;

            public Continuation(Counter lastCount, Counter lastLabelCount)
            {
                LastCount = lastCount;
                LastLabelCount = lastLabelCount;
            }
        }

        private class CallInliner : BoundTreeRewriter
        {
            private readonly BoundCallStatement _call;
            private readonly Inliner _treeInliner;
            private readonly FunctionSymbol _func;
            private readonly bool _assignReturns;
            private readonly Dictionary<VariableSymbol, object> _inlinedVariables = [];

            private Counter _varCount;
            private Counter _labelCount;

            private CallInliner(BoundCallStatement call, Inliner treeInliner, bool assignReturns)
            {
                _call = call;
                _treeInliner = treeInliner;
                _func = _call.Function;
                _assignReturns = assignReturns;
            }

            public static BoundBlockStatement Inline(BoundCallStatement call, Inliner treeInliner, bool assignReturns, ref Counter varCount, ref Counter labelCount)
            {
                CallInliner inliner = new CallInliner(call, treeInliner, assignReturns);
                return Flatten(inliner.Inline(ref varCount, ref labelCount));
            }

            private static BoundBlockStatement Flatten(BoundStatement statement)
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
                        {
                            stack.Push(s);
                        }
                    }
                    else
                    {
                        builder.Add(current);
                    }
                }

                return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
            protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
            {
                if (node.Variable is BasicVariableSymbol)
                {
                    BoundExpression ex = GetInlinedVar(node.Variable, node.Syntax);
                    BoundExpression rewritten = RewriteExpression(node.Expression);
                    return ex is BoundVariableExpression varEx ? Assignment(node.Syntax, varEx.Variable, rewritten) : new BoundNopStatement(node.Syntax);
                }
                else
                {
                    return base.RewriteAssignmentStatement(node);
                }
            }

            protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
                => node.Variable is BasicVariableSymbol ? GetInlinedVar(node.Variable, node.Syntax) : base.RewriteVariableExpression(node);

            protected override BoundStatement RewriteReturnStatement(BoundReturnStatement node)
                => node.Expression is null || !_assignReturns
                    ? Goto(_call.Syntax, new BoundLabel("funcEnd" + _labelCount.ToString()))
                    : Block(
                        node.Syntax,
                        Assignment(node.Syntax, ReservedCompilerVariableSymbol.CreateFunctionRes(_func, true), RewriteExpression(node.Expression)),
                        Goto(_call.Syntax, new BoundLabel("funcEnd" + _labelCount.ToString())));

            private BoundBlockStatement Inline(ref Counter varCount, ref Counter labelCount)
            {
                _varCount = varCount;
                _labelCount = labelCount;

                SyntaxNode syntax = _call.Syntax;
                List<BoundStatement> statements = new List<BoundStatement>(_func.Parameters.Length + 1);

                // assign params to args
                for (int i = 0; i < _func.Parameters.Length; i++)
                {
                    ParameterSymbol param = _func.Parameters[i];

                    if (_call.Arguments[i] is BoundVariableExpression varEx &&
                        (param.Modifiers.HasOneOfFlags(Modifiers.Ref, Modifiers.Out) ||
                        (param.Modifiers.HasFlag(Modifiers.Readonly) && varEx.Variable is BasicVariableSymbol)))
                    {
                        _inlinedVariables.Add(param, varEx.Variable);
                    }
                    else if (_call.Arguments[i] is BoundLiteralExpression literal)
                    {
                        _inlinedVariables.Add(param, literal);
                    }
                    else
                    {
                        _inlinedVariables.Add(param, ReservedCompilerVariableSymbol.CreateParam(_func, i));

                        statements.Add(
                            Assignment(
                                syntax,
                                _func.Parameters[i],
                                _call.Arguments[i]));
                    }
                }

                statements.Add(RewriteStatement(_treeInliner._functions[_func]));
                statements.Add(Label(_call.Syntax, new BoundLabel("funcEnd" + _labelCount.ToString())));
                _labelCount++;

                // don't need to asign out args, as they were used dirrectly
                if (_func.Type != TypeSymbol.Void && _func is not BuiltinFunctionSymbol && _call.ResultVariable is not null)
                {
                    statements.Add(Assignment(_call.Syntax, _call.ResultVariable, Variable(_call.Syntax, ReservedCompilerVariableSymbol.CreateFunctionRes(_func, true)))); // assign ret
                }

                BoundBlockStatement block = Block(
                    syntax,
                    [.. statements]);

                varCount = _varCount;
                labelCount = _labelCount;

                return block;
            }

            private BoundExpression GetInlinedVar(VariableSymbol variable, SyntaxNode syntax)
            {
                if (variable.IsGlobal || (variable is ReservedCompilerVariableSymbol reserved && (reserved.Identifier == "inl" || reserved.Identifier == "temp")))
                {
                    return Variable(syntax, variable);
                }
                else if (_inlinedVariables.TryGetValue(variable, out object? inlined))
                {
                    return inlined is BoundExpression ex ? ex : Variable(syntax, (VariableSymbol)inlined);
                }
                else
                {
                    inlined = new ReservedCompilerVariableSymbol("inl", _varCount.ToString(), variable.Modifiers, variable.Type);
                    _inlinedVariables.Add(variable, inlined);

                    _varCount++;

                    return Variable(syntax, (VariableSymbol)inlined);
                }
            }
        }
    }
}
