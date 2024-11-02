using FanScript.Compiler.Symbols.Functions;

namespace FanScript.Utils
{
    internal sealed class CircularCallDetector
    {
        private readonly SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> _callGraph;

        public CircularCallDetector(SetMultiValueDictionary<FunctionSymbol, FunctionSymbol> callGraph)
        {
            _callGraph = callGraph;
        }

        /// <summary>
        /// Finds cycles in function calls (for example: func1 -> func1; func1 -> func2 -> func3 -> func1)
        /// </summary>
        /// <returns>The dected cycle, or null if none were found</returns>
        public IEnumerable<FunctionSymbol>? Detect()
        {
            HashSet<FunctionSymbol> visited = [];
            List<FunctionSymbol> recursionStack = [];

            foreach (FunctionSymbol function in _callGraph.Keys)
            {
                IEnumerable<FunctionSymbol>? cycle = DetectCycle(function, visited, recursionStack);

                if (cycle is not null)
                {
                    return cycle;
                }
            }

            return null;
        }

        private IEnumerable<FunctionSymbol>? DetectCycle(FunctionSymbol function, HashSet<FunctionSymbol> visited, List<FunctionSymbol> recursionStack)
        {
            if (recursionStack.Contains(function))
            {
                return recursionStack
                    .SkipWhile(f => f != function)
                    .Concat([function]);
            }

            if (!visited.Add(function))
            {
                return null; // if visited already contained function
            }

            recursionStack.Add(function);

            if (_callGraph.ContainsKey(function))
            {
                foreach (var caller in _callGraph[function])
                {
                    IEnumerable<FunctionSymbol>? cycle = DetectCycle(caller, visited, recursionStack);

                    if (cycle is not null)
                    {
                        return cycle;
                    }
                }
            }

            recursionStack.Remove(function);

            return null;
        }
    }
}
