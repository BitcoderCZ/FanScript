using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundScope
    {
        private Dictionary<string, VariableSymbol> variables = new();
        private Dictionary<string, List<FunctionSymbol>> functions = new();

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public BoundScope? Parent { get; }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            if (VariablelExists(variable))
                return false;

            variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (FunctionExists(function))
                return false;

            if (!functions.TryGetValue(function.Name, out var symbolList))
            {
                symbolList = new();
                functions.Add(function.Name, symbolList);
            }

            symbolList.Add(function);
            return true;
        }

        private bool VariablelExists(VariableSymbol variable)
        {
            if (variables.ContainsKey(variable.Name)) return true;

            return Parent?.VariablelExists(variable) ?? false;
        }

        private bool FunctionExists(FunctionSymbol function)
        {
            if (functions.TryGetValue(function.Name, out var symbolList))
            {
                if (symbolList.Where(symbol => symbol is FunctionSymbol)
                       .Where(func => Enumerable.SequenceEqual(
                           func.Parameters.Select(param => param.Type),
                           function.Parameters.Select(param => param.Type))
                       )
                       .Count() != 0)
                    return true;
            }

            return Parent?.FunctionExists(function) ?? false;
        }

        public Symbol? TryLookupVariable(string name)
        {
            if (variables.TryGetValue(name, out var variable))
                return variable;

            return Parent?.TryLookupVariable(name);
        }

        public FunctionSymbol? TryLookupFunction(string name, IEnumerable<TypeSymbol> arguments, bool method)
        {
            int argumentCount = arguments.Count();

            if (functions.TryGetValue(name, out var list))
                foreach (var function in list)
                    if ((!method || function.IsMethod) &&
                        function.Parameters
                            .Select(param => param.Type)
                            .SequenceEqual(arguments, new TypeSymbol.FuntionParamsComparer()))
                        return function;

            FunctionSymbol? result = Parent?.TryLookupFunction(name, arguments, method);
            if (result is not null)
                return result;

            if (functions.TryGetValue(name, out var funcs))
                return funcs
                    .Where(func => func.Name == name && (!method || func.IsMethod))
                    .OrderBy(func =>
                    {
                        int diff = func.Parameters.Length - argumentCount;
                        if (diff < 0)
                            return Math.Abs(diff) + 2; // make funcs with lees params than args choosen less

                        return diff;
                    })
                    .FirstOrDefault();

            return null;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
            => variables.Values.ToImmutableArray();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
            => functions.Values.SelectMany(list => list).ToImmutableArray();
    }
}
