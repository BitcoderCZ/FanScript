using FanScript.Compiler.Symbols;

namespace FanScript.Utils
{
    internal sealed class CircularCallDetector
    {
        private SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph;

        public CircularCallDetector(SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph)
        {
            this.callGraph = callGraph;
        }

        /// <summary>
        /// Finds cycles in function calls (for example: func1 -> func1; func1 -> func2 -> func3 -> func1)
        /// </summary>
        /// <returns>The dected cycle, or null if none were found</returns>
        public IEnumerable<FunctionSymbol>? Detect()
        {
            HashSet<FunctionSymbol> visited = new();
            List<FunctionSymbol> recursionStack = new();

            foreach (FunctionSymbol function in callGraph.Keys)
            {
                IEnumerable<FunctionSymbol>? cycle = detectCycle(function, visited, recursionStack);

                if (cycle is not null)
                    return cycle;
            }

            return null;
        }

        private IEnumerable<FunctionSymbol>? detectCycle(FunctionSymbol function, HashSet<FunctionSymbol> visited, List<FunctionSymbol> recursionStack)
        {
            if (recursionStack.Contains(function))
            {
                return recursionStack
                    .SkipWhile(f => f != function)
                    .Concat([function]);
            }

            if (!visited.Add(function))
                return null; // if visited already contained function

            recursionStack.Add(function);

            if (callGraph.ContainsKey(function))
            {
                foreach (var caller in callGraph[function])
                {
                    IEnumerable<FunctionSymbol>? cycle = detectCycle(caller, visited, recursionStack);

                    if (cycle is not null)
                        return cycle;
                }
            }

            recursionStack.Remove(function);

            return null;
        }
    }
}
