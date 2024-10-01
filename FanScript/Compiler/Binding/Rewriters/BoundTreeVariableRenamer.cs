using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters
{
    /// <summary>
    /// Renames all variables, makes sure variables with the same name in differend scopes don't "collide"
    /// </summary>
    internal sealed class BoundTreeVariableRenamer : BoundTreeRewriter
    {
        private readonly Dictionary<VariableSymbol, VariableSymbol> renamedDict = new();
        private Counter varCount = new Counter(0);

        public BoundTreeVariableRenamer(Continuation? continuation = null)
        {
            if (continuation is not null)
                varCount = continuation.Value.LastCount;
        }

        public static BoundBlockStatement RenameVariables(BoundBlockStatement statement, ref Continuation? continuation)
        {
            BoundTreeVariableRenamer unifier = new BoundTreeVariableRenamer(continuation);
            BoundStatement res = unifier.RewriteBlockStatement(statement);

            continuation = new Continuation(unifier.varCount);

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
                renamed = new CompilerVariableSymbol(varCount.ToString(), variable.Modifiers, variable.Type);
                renamedDict.Add(variable, renamed);

                varCount++;

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
