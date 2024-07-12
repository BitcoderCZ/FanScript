using FanScript.Compiler.Syntax;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Symbols
{
    public class FunctionSymbol : Symbol
    {
        internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FunctionDeclarationSyntax? declaration = null)
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
        }
        internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, ImmutableArray<TypeSymbol>? allowedGenericTypes, FunctionDeclarationSyntax? declaration = null)
            : base(name)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
            IsGeneric = true;
            AllowedGenericTypes = allowedGenericTypes;
        }

        public override SymbolKind Kind => SymbolKind.Function;
        public FunctionDeclarationSyntax? Declaration { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        [MemberNotNullWhen(true, nameof(AllowedGenericTypes))]
        public bool IsGeneric { get; }
        public ImmutableArray<TypeSymbol>? AllowedGenericTypes { get; }
    }
}
