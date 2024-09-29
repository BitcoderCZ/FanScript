using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using System.Collections.Immutable;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    internal sealed class BoundTreeInliner : BoundTreeRewriter
    {
        private readonly BoundAnalysisResult analysisResult;
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions;

        // TODO: inlined cache dict FunctionSymbol, BlockStatement

        private int varCount = 0;

        public BoundTreeInliner(BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, Continuation? continuation = null)
        {
            this.analysisResult = analysisResult;
            this.functions = functions;

            if (continuation is not null)
                varCount = continuation.Value.LastCount;
            this.functions = functions;
        }

        public static BoundBlockStatement Inline(BoundBlockStatement statement, BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref Continuation? continuation)
        {
            BoundTreeInliner inliner = new BoundTreeInliner(analysisResult, functions, continuation);
            BoundStatement res = inliner.RewriteBlockStatement(statement);

            continuation = new Continuation(inliner.varCount);

            return res is BoundBlockStatement blockRes ? blockRes : new BoundBlockStatement(res.Syntax, [res]);
        }

        protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            if (analysisResult.ShouldFunctionGetInlined(node.Function))
                return new BoundStatementExpression(node.Syntax, CallInliner.Inline(node, this, ref varCount));
            else
                return base.RewriteCallExpression(node);
        }

        public struct Continuation
        {
            public readonly int LastCount;

            public Continuation(int lastCount)
            {
                LastCount = lastCount;
            }
        }

        private class CallInliner : BoundTreeRewriter
        {
            private readonly BoundCallExpression call;
            BoundTreeInliner treeInliner;
            private readonly FunctionSymbol func;
            private int varCount;

            private readonly Dictionary<VariableSymbol, VariableSymbol> inlinedVariables = new();

            private CallInliner(BoundCallExpression call, BoundTreeInliner treeInliner)
            {
                this.call = call;
                this.treeInliner = treeInliner;
                func = this.call.Function;
            }

            public static BoundStatement Inline(BoundCallExpression call, BoundTreeInliner treeInliner, ref int varCount)
            {
                CallInliner inliner = new CallInliner(call, treeInliner);
                return inliner.Inline(ref varCount);
            }

            private BoundStatement Inline(ref int varCount)
            {
                this.varCount = varCount;

                Syntax.SyntaxNode syntax = call.Syntax;
                List<BoundStatement> statements = new List<BoundStatement>(func.Parameters.Length + 1);

                // TODO: if a param is readonly and the arg is a variable, references to the param can be replaced with the arg variable

                // assign params to args
                for (int i = 0; i < func.Parameters.Length; i++)
                {
                    statements.Add(
                        Assignment(
                            syntax,
                            getInlinedVar(func.Parameters[i]),
                            call.Arguments[i]
                        )
                    );
                }

                statements.Add(RewriteStatement(treeInliner.functions[func]));

                // assign out args
                for (int i = 0; i < call.Arguments.Length; i++)
                {
                    var param = func.Parameters[i];
                    var arg = call.Arguments[i];

                    if (param.Modifiers.HasFlag(Modifiers.Out))
                    {
                        statements.Add(
                            Assignment(
                                syntax,
                                ((BoundVariableExpression)arg).Variable,
                                Variable(syntax, getInlinedVar(param))
                            )
                        );
                    }
                }

                BoundBlockStatement block = Block(
                    syntax,
                    statements.ToArray()
                );

                varCount = this.varCount;

                return block;
            }

            protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
            {
                if (node.Variable is BasicVariableSymbol)
                    return Assignment(node.Syntax, getInlinedVar(node.Variable), RewriteExpression(node.Expression));
                else
                    return base.RewriteAssignmentStatement(node);
            }

            protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
            {
                if (node.Variable is BasicVariableSymbol)
                    return Variable(node.Syntax, getInlinedVar(node.Variable));
                else
                    return base.RewriteVariableExpression(node);
            }

            private VariableSymbol getInlinedVar(VariableSymbol variable)
            {
                if (variable.IsGlobal || variable is ReservedCompilerVariableSymbol reserved && reserved.Identifier == "inl")
                    return variable;
                else if (inlinedVariables.TryGetValue(variable, out var inlined))
                    return inlined;
                else
                {
                    inlined = new ReservedCompilerVariableSymbol("inl", varCount.ToString(), variable.Modifiers, variable.Type);
                    inlinedVariables.Add(variable, inlined);

                    varCount++;

                    return inlined;
                }
            }
        }
    }
}
