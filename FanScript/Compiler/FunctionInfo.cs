using FanScript.Compiler.Symbols;
using System.Collections.Immutable;

namespace FanScript.Compiler
{
    public class FunctionInfo
    {
        public readonly int CallCount;
        public readonly ImmutableArray<VariableSymbol> LocalVariables;

        public FunctionInfo()
        {
            CallCount = 0;
            LocalVariables = ImmutableArray<VariableSymbol>.Empty;
        }
        public FunctionInfo(ImmutableArray<VariableSymbol> localVariables, int callCount)
        {
            LocalVariables = localVariables;
            CallCount = callCount;
        }
    }
}
