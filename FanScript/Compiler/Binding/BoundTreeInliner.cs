using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundTreeInliner : BoundTreeRewriter
    {
        private readonly BoundAnalysisResult analysisResult;
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions;

        // TODO: inlined cache dict FunctionSymbol, BlockStatement

        private int varCount = 0;
        public InlineContinuation Continuation => new InlineContinuation(varCount);

        public BoundTreeInliner(BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, InlineContinuation? continuation = null)
        {
            this.analysisResult = analysisResult;
            this.functions = functions;

            if (continuation is not null)
                varCount = continuation.Value.LastCount;
            this.functions = functions;
        }

        public static BoundStatement Inline(BoundStatement statement, BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref InlineContinuation? continuation)
        {
            BoundTreeInliner inliner = new BoundTreeInliner(analysisResult, functions, continuation);
            BoundStatement res = inliner.RewriteStatement(statement);

            continuation = new InlineContinuation(inliner.varCount);

            return res;
        }

        protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            if (analysisResult.ShouldFunctionGetInlined(node.Function))
                return new BoundStatementExpression(node.Syntax, CallInliner.Inline(node, this, ref varCount));
            else
                return base.RewriteCallExpression(node);
        }

        public struct InlineContinuation
        {
            public readonly int LastCount;

            public InlineContinuation(int lastCount)
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
                if (node.Variable is LocalVariableSymbol or GlobalVariableSymbol)
                    return Assignment(node.Syntax, getInlinedVar(node.Variable), RewriteExpression(node.Expression));
                else
                    return base.RewriteAssignmentStatement(node);
            }

            protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
            {
                if (node.Variable is LocalVariableSymbol or GlobalVariableSymbol)
                    return Variable(node.Syntax, getInlinedVar(node.Variable));
                else
                    return base.RewriteVariableExpression(node);
            }

            private VariableSymbol getInlinedVar(VariableSymbol variable)
            {
                if (variable.Name.StartsWith("^^inl"))
                    return variable;
                else if (inlinedVariables.TryGetValue(variable, out var inlined))
                    return inlined;
                else
                {
                    if (variable is GlobalVariableSymbol)
                        return variable;

                    inlined = new LocalVariableSymbol("^^inl" + varCount, variable.Modifiers, variable.Type);
                    inlinedVariables.Add(variable, inlined);

                    varCount++;

                    return inlined;
                }
            }
        }
    }
}
