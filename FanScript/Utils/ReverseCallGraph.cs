using FanScript.Compiler.Symbols;

namespace FanScript.Utils
{
    // a { b, c, d } b { c } c { d } d { } -> d, c, b, a
    internal sealed class ReverseCallGraph
    {
        private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph;

        public ReverseCallGraph(SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph)
        {
            this.callGraph = callGraph;
        }

        public IEnumerable<FunctionSymbol> GetCallOrder()
        {
            HashSet<FunctionSymbol> visited = new();
            Stack<FunctionSymbol> stack = new();

            foreach (var function in callGraph.Keys)
            {
                if (!visited.Contains(function))
                    dfs(function, visited, stack);
            }

            return stack.Reverse();
        }

        private void dfs(FunctionSymbol function, HashSet<FunctionSymbol> visited, Stack<FunctionSymbol> stack)
        {
            visited.Add(function);

            foreach (var func in callGraph.Where(kvp => kvp.Value.Contains(function)).Select(kvp => kvp.Key))
            {
                if (!visited.Contains(func))
                    dfs(func, visited, stack);
            }

            stack.Push(function);
        }
    }
}
