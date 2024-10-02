using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Variables;
using System.Collections.Immutable;

namespace FanScript.Compiler.Symbols
{
    internal sealed class BuiltinFunctionSymbol : FunctionSymbol
    {
        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, Func<BoundCallExpression, IEmitContext, EmitStore> emit) : base(0, type, name, parameters)
        {
            Emit = emit;
        }

        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes, Func<BoundCallExpression, IEmitContext, EmitStore> emit) : base(0, type, name, parameters, allowedGenericTypes)
        {
            Emit = emit;
        }

        public Func<BoundCallExpression, IEmitContext, EmitStore> Emit { get; }
    }
}
