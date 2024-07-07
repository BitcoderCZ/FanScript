using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram(BoundProgram? previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? scriptFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            ScriptFunction = scriptFunction;
            Functions = functions;
        }

        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
    }
}
