using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using System.Collections.Immutable;

namespace FanScript.Compiler.Symbols
{
    internal sealed class BuiltinFunctionSymbol : FunctionSymbol
    {
        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, Func<BoundCallExpression, EmitContext, EmitStore> emit) : base(name, parameters, type)
        {
            Emit = emit;
        }

        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes, Func<BoundCallExpression, EmitContext, EmitStore> emit) : base(name, parameters, type, allowedGenericTypes)
        {
            Emit = emit;
        }

        public Func<BoundCallExpression, EmitContext, EmitStore> Emit { get; }
    }
}
