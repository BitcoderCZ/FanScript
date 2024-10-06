﻿using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Collections.Immutable;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    internal sealed class Inliner : BoundTreeRewriter
    {
        private readonly BoundAnalysisResult analysisResult;
        private readonly ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions;

        private Counter varCount = new Counter(0);

        public Inliner(BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, Continuation? continuation = null)
        {
            this.analysisResult = analysisResult;
            this.functions = functions;

            if (continuation is not null)
                varCount = continuation.Value.LastCount;
            this.functions = functions;
        }

        public static BoundBlockStatement Inline(BoundBlockStatement statement, BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref Continuation? continuation)
        {
            Inliner inliner = new Inliner(analysisResult, functions, continuation);
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
            public readonly Counter LastCount;

            public Continuation(Counter lastCount)
            {
                LastCount = lastCount;
            }
        }

        private class CallInliner : BoundTreeRewriter
        {
            private readonly BoundCallExpression call;
            Inliner treeInliner;
            private readonly FunctionSymbol func;
            private Counter varCount;

            private readonly Dictionary<VariableSymbol, VariableSymbol> inlinedVariables = new();

            private CallInliner(BoundCallExpression call, Inliner treeInliner)
            {
                this.call = call;
                this.treeInliner = treeInliner;
                func = this.call.Function;
            }

            public static BoundStatement Inline(BoundCallExpression call, Inliner treeInliner, ref Counter varCount)
            {
                CallInliner inliner = new CallInliner(call, treeInliner);
                return inliner.Inline(ref varCount);
            }

            private BoundStatement Inline(ref Counter varCount)
            {
                this.varCount = varCount;

                Syntax.SyntaxNode syntax = call.Syntax;
                List<BoundStatement> statements = new List<BoundStatement>(func.Parameters.Length + 1);

                // assign params to args
                for (int i = 0; i < func.Parameters.Length; i++)
                {
                    if (func.Parameters[i].Modifiers.HasFlag(Modifiers.Readonly) && call.Arguments[i] is BoundVariableExpression varEx && varEx.Variable is BasicVariableSymbol)
                        inlinedVariables.Add(func.Parameters[i], varEx.Variable);

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