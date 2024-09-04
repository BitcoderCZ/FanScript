using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram(BoundProgram? previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? scriptFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions,
                            ImmutableDictionary<FunctionSymbol, FunctionInfo> functionInfos)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            ScriptFunction = scriptFunction;
            Functions = functions;
            FunctionInfos = functionInfos;
        }

        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public ImmutableDictionary<FunctionSymbol, FunctionInfo> FunctionInfos { get; }
    }
}
