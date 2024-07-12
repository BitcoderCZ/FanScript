using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Symbols
{
    internal sealed class BuiltinFunctionSymbol : FunctionSymbol
    {
        internal BuiltinFunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FunctionDeclarationSyntax? declaration = null) : base(name, parameters, type, declaration)
        {
        }

        public Func<BoundCallExpression, EmitContext, EmitStore>? Emit { get; init; }
    }
}
