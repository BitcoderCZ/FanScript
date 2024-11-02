using System.Collections.Immutable;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Variables;

namespace FanScript.Compiler.Symbols.Functions
{
    internal class BuiltinFunctionSymbol : FunctionSymbol
    {
        internal BuiltinFunctionSymbol(Namespace @namespace, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, Func<BoundCallExpression, IEmitContext, IEmitStore> emit)
            : base(@namespace, 0, type, name, parameters)
        {
            Emit = emit;
        }

        internal BuiltinFunctionSymbol(Namespace @namespace, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes, Func<BoundCallExpression, IEmitContext, IEmitStore> emit)
            : base(@namespace, 0, type, name, parameters, allowedGenericTypes)
        {
            Emit = emit;
        }

        public Func<BoundCallExpression, IEmitContext, IEmitStore> Emit { get; }
    }
}
