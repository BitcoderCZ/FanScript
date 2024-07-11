using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
