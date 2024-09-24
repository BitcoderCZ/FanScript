using FanScript.Compiler.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundTreeInliner : BoundTreeRewriter
    {
        private readonly BoundAnalysisResult analysisResult;
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions;

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

        protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            FunctionSymbol func = node.Function;

            if (func.Modifiers.HasFlag(Modifiers.Inline) || analysisResult.GetCallCount(func) < 2)
                return new BoundStatementExpression(node.Syntax, CallInliner.Inline(node, functions, ref varCount));
            else
                return node;
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
            private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions;
            private readonly FunctionSymbol func;
            private int varCount;

            private readonly Dictionary<VariableSymbol, VariableSymbol> inlinedVariables = new();

            private CallInliner(BoundCallExpression call, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
            {
                this.call = call;
                this.functions = functions;
                func = this.call.Function;
            }

            public static BoundStatement Inline(BoundCallExpression call, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref int varCount)
            {
                CallInliner inliner = new CallInliner(call, functions);
                return inliner.Inline(ref varCount);
            }

            private BoundStatement Inline(ref int varCount)
            {
                this.varCount = varCount;

                Syntax.SyntaxNode syntax = call.Syntax;
                List<BoundStatement> statements = new List<BoundStatement>(func.Parameters.Length + 1);

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

                statements.Add(RewriteStatement(functions[func]));

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
                        inlined = new GlobalVariableSymbol("^^inl" + varCount, variable.Modifiers, variable.Type);
                    else
                        inlined = new LocalVariableSymbol("^^inl" + varCount, variable.Modifiers, variable.Type);

                    varCount++;

                    return inlined;
                }
            }
        }
    }
}
