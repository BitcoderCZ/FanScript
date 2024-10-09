using FanScript.Compiler.Symbols;
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
        private Counter labelCount = new Counter(0);

        public Inliner(BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, Continuation? continuation = null)
        {
            this.analysisResult = analysisResult;
            this.functions = functions;

            if (continuation is not null)
            {
                varCount = continuation.Value.LastCount;
                labelCount = continuation.Value.LastLabelCount;
            }
        }

        public static BoundBlockStatement Inline(BoundBlockStatement statement, BoundAnalysisResult analysisResult, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ref Continuation? continuation)
        {
            Inliner inliner = new Inliner(analysisResult, functions, continuation);
            BoundStatement res = inliner.RewriteBlockStatement(statement);

            continuation = new Continuation(inliner.varCount, inliner.labelCount);

            return res is BoundBlockStatement blockRes ? blockRes : new BoundBlockStatement(res.Syntax, [res]);
        }

        protected override BoundStatement RewriteCallStatement(BoundCallStatement node)
        {
            if (analysisResult.ShouldFunctionGetInlined(node.Function))
                return CallInliner.Inline(node, this, node.ResultVariable is not null, ref varCount, ref labelCount);
            else
                return base.RewriteCallStatement(node);
        }

        public struct Continuation
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
            private readonly BoundCallStatement call;
            private readonly Inliner treeInliner;
            private readonly FunctionSymbol func;
            private readonly bool assignReturns;
            private Counter varCount;
            private Counter labelCount;

            private readonly Dictionary<VariableSymbol, VariableSymbol> inlinedVariables = new();

            private CallInliner(BoundCallStatement call, Inliner treeInliner, bool assignReturns)
            {
                this.call = call;
                this.treeInliner = treeInliner;
                func = this.call.Function;
                this.assignReturns = assignReturns;
            }

            public static BoundStatement Inline(BoundCallStatement call, Inliner treeInliner, bool assignReturns, ref Counter varCount, ref Counter labelCount)
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
                            stack.Push(s);
                    }
                    else
                        builder.Add(current);
                }

                return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
            }

            private BoundStatement Inline(ref Counter varCount, ref Counter labelCount)
            {
                this.varCount = varCount;
                this.labelCount = labelCount;

                Syntax.SyntaxNode syntax = call.Syntax;
                List<BoundStatement> statements = new List<BoundStatement>(func.Parameters.Length + 1);

                // assign params to args
                for (int i = 0; i < func.Parameters.Length; i++)
                {
                    ParameterSymbol param = func.Parameters[i];

                    if (call.Arguments[i] is BoundVariableExpression varEx &&
                        (param.Modifiers.HasOneOfFlags(Modifiers.Ref, Modifiers.Out) ||
                        (param.Modifiers.HasFlag(Modifiers.Readonly) && varEx.Variable is BasicVariableSymbol)))
                        inlinedVariables.Add(param, varEx.Variable);
                    else
                    {
                        inlinedVariables.Add(param, ReservedCompilerVariableSymbol.CreateParam(func, i));

                        statements.Add(
                            Assignment(
                                syntax,
                                getInlinedVar(func.Parameters[i]),
                                call.Arguments[i]
                            )
                        );
                    }
                }

                statements.Add(RewriteStatement(treeInliner.functions[func]));
                statements.Add(Label(call.Syntax, new BoundLabel("funcEnd" + this.labelCount.ToString())));
                this.labelCount++;

                // don't need to asign out args, as they were used dirrectly

                if (func.Type != TypeSymbol.Void && func is not BuiltinFunctionSymbol && call.ResultVariable is not null)
                    statements.Add(Assignment(call.Syntax, call.ResultVariable, Variable(call.Syntax, ReservedCompilerVariableSymbol.CreateFunctionRes(func, true)))); // assign ret

                BoundBlockStatement block = Block(
                    syntax,
                    statements.ToArray()
                );

                varCount = this.varCount;
                labelCount = this.labelCount;

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

            protected override BoundStatement RewriteReturnStatement(BoundReturnStatement node)
            {
                if (node.Expression is null || !assignReturns)
                    return Goto(call.Syntax, new BoundLabel("funcEnd" + labelCount.ToString()));

                return Block(
                    node.Syntax,
                    Assignment(node.Syntax, ReservedCompilerVariableSymbol.CreateFunctionRes(func, true), RewriteExpression(node.Expression)),
                    Goto(call.Syntax, new BoundLabel("funcEnd" + labelCount.ToString()))
                );
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
