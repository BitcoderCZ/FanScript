using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope? previous,
                                ImmutableArray<Diagnostic> diagnostics,
                                FunctionSymbol? scriptFunction,
                                ImmutableArray<FunctionSymbol> functions,
                                ImmutableArray<VariableSymbol> variables,
                                ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            ScriptFunction = scriptFunction;
            Functions = functions;
            Variables = variables;
            Statements = statements;
        }

        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<BoundStatement> Statements { get; }
    }
}
