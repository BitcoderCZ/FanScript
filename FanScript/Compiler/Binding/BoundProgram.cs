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
                            BoundAnalysisResult analysis)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            ScriptFunction = scriptFunction;
            Functions = functions;
            Analysis = analysis;
        }

        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public BoundAnalysisResult Analysis { get; }
    }
}
