using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Variables;
using System.Collections.Immutable;

namespace FanScript.Compiler.Symbols
{
    internal sealed class ConstantFunctionSymbol : BuiltinFunctionSymbol
    {
        internal ConstantFunctionSymbol(Namespace @namespace, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, Func<BoundCallExpression, IEmitContext, EmitStore> emit, Func<BoundConstant[], object[]> constantEmit) : base(@namespace, name, parameters, type, emit)
        {
            ConstantEmit = constantEmit;
        }

        internal ConstantFunctionSymbol(Namespace @namespace, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes, Func<BoundCallExpression, IEmitContext, EmitStore> emit, Func<BoundConstant[], object[]> constantEmit) : base(@namespace, name, parameters, type, allowedGenericTypes, emit)
        {
            ConstantEmit = constantEmit;
        }

        public Func<BoundConstant[], object[]> ConstantEmit { get; }
    }
}
