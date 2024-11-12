using FanScript.Compiler.Symbols.Functions;

namespace FanScript.Utils;

// a { b, c, d } b { c } c { d } d { } -> d, c, b, a
internal sealed class ReverseCallGraph
{
    private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> _callGraph;

    public ReverseCallGraph(SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph)
    {
        _callGraph = callGraph;
    }

    public IEnumerable<FunctionSymbol> GetCallOrder()
    {
        HashSet<FunctionSymbol> visited = [];
        Stack<FunctionSymbol> stack = new();

        foreach (var function in _callGraph.Keys)
        {
            if (!visited.Contains(function))
            {
                Dfs(function, visited, stack);
            }
        }

        return stack.Reverse();
    }

    private void Dfs(FunctionSymbol function, HashSet<FunctionSymbol> visited, Stack<FunctionSymbol> stack)
    {
        visited.Add(function);

        foreach (var func in _callGraph.Where(kvp => kvp.Value.Contains(function)).Select(kvp => kvp.Key))
        {
            if (!visited.Contains(func))
            {
                Dfs(func, visited, stack);
            }
        }

        stack.Push(function);
    }
}
