using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Symbols
{
    internal sealed class BuiltinFunctionSymbol : FunctionSymbol
    {
        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type) : base(name, parameters, type)
        {
        }

        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes) : base(name, parameters, type, allowedGenericTypes)
        {
        }

        public Func<BoundCallExpression, EmitContext, EmitStore>? Emit { get; init; }
    }
}
