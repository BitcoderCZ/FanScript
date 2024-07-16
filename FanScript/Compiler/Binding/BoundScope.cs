using FanScript.Compiler.Symbols;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundScope
    {
        private Dictionary<string, List<Symbol>>? _symbols;

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public BoundScope? Parent { get; }

        public bool TryDeclareVariable(VariableSymbol variable)
            => TryDeclareSymbol(variable);

        public bool TryDeclareFunction(FunctionSymbol function)
            => TryDeclareSymbol(function);

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol)
            where TSymbol : Symbol
        {
            if (_symbols is null)
                _symbols = new();
            else if (_symbols.TryGetValue(symbol.Name, out var symbolList))
            {
                if (symbol is FunctionSymbol function)
                {
                    if (symbolList.Where(symbol => symbol is FunctionSymbol)
                        .Select(symbol => (FunctionSymbol)symbol)
                        .Where(func => Enumerable.SequenceEqual(
                            func.Parameters.Select(param => param.Type),
                            function.Parameters.Select(param => param.Type))
                        )
                        .Count() == 0)
                    { // function(s) with same name, but differend parameters
                        symbolList.Add(symbol);
                        return true;
                    }
                }

                return false;
            }

            _symbols.Add(symbol.Name, [symbol]);
            return true;
        }

        public Symbol? TryLookupSymbol(string name)
        {
            if (_symbols is not null && _symbols.TryGetValue(name, out var symbolList))
                return symbolList.FirstOrDefault();

            return Parent?.TryLookupSymbol(name);
        }

        public FunctionSymbol? TryLookupFunction(string name, IEnumerable<TypeSymbol> arguments)
        {
            if (_symbols is not null && _symbols.TryGetValue(name, out var symbolList))
                foreach (var symbol in symbolList)
                    if (symbol is FunctionSymbol function && function.Parameters
                        .Select(param => param.Type)
                        .SequenceEqual(arguments, new TypeSymbol.FuntionParamsComparer()))
                        return function;

            return Parent?.TryLookupFunction(name, arguments);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
            => GetDeclaredSymbols<VariableSymbol>();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
            => GetDeclaredSymbols<FunctionSymbol>();

        private ImmutableArray<TSymbol> GetDeclaredSymbols<TSymbol>()
            where TSymbol : Symbol
        {
            if (_symbols is null)
                return ImmutableArray<TSymbol>.Empty;

            return _symbols.Values.SelectMany(symbols => symbols).OfType<TSymbol>().ToImmutableArray();
        }
    }
}
