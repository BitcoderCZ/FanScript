using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Diagnostics;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    /// <summary>
    /// Renames all variables, makes sure variables with the same name in differend scopes don't "collide"
    /// </summary>
    internal sealed class BoundTreeVariableRenamer : BoundTreeRewriter
    {
        private readonly FunctionSymbol function;
        private readonly Dictionary<VariableSymbol, VariableSymbol> renamedDict = new();
        private Counter varCount = new Counter(0);

        public BoundTreeVariableRenamer(FunctionSymbol function, Continuation? continuation = null)
        {
            this.function = function;

            if (continuation is not null)
                varCount = continuation.Value.LastCount;
        }

        public static BoundBlockStatement RenameVariables(BoundBlockStatement statement, FunctionSymbol function, ref Continuation? continuation)
        {
            BoundTreeVariableRenamer renamer = new BoundTreeVariableRenamer(function, continuation);
            BoundStatement res = renamer.RewriteBlockStatement(statement);

            continuation = new Continuation(renamer.varCount);

            return res is BoundBlockStatement blockRes ? blockRes : new BoundBlockStatement(statement.Syntax, [statement]);
        }
        protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            if (node.Variable is BasicVariableSymbol)
                return Assignment(node.Syntax, getRenamedVar(node.Variable), RewriteExpression(node.Expression));
            else
                return base.RewriteAssignmentStatement(node);
        }

        protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            if (node.Variable is BasicVariableSymbol)
                return Variable(node.Syntax, getRenamedVar(node.Variable));
            else
                return base.RewriteVariableExpression(node);
        }

        private VariableSymbol getRenamedVar(VariableSymbol variable)
        {
            if (variable.IsGlobal || variable is ParameterSymbol)
                return variable;
            else if (renamedDict.TryGetValue(variable, out var renamed))
                return renamed;
            else
            {
                if (variable is ParameterSymbol param)
                {
                    int paramIndex = function.Parameters.IndexOf(param);

                    Debug.Assert(paramIndex >= 0);

                    renamed = new ReservedCompilerVariableSymbol("func" + function.Id.ToString(), paramIndex.ToString(), param.Modifiers, param.Type);
                }
                else
                {
                    renamed = new CompilerVariableSymbol(varCount.ToString(), variable.Modifiers, variable.Type);
                    varCount++;
                }

                renamedDict.Add(variable, renamed);

                return renamed;
            }
        }

        public struct Continuation
        {
            public readonly Counter LastCount;

            public Continuation(Counter lastCount)
            {
                LastCount = lastCount;
            }
        }
    }
}
