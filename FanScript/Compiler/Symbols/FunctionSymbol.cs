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

        public string? Description { get; init; }

        public string ToString(bool onlyParams)
        {
            if (!onlyParams)
                return ToString();

            using (var writer = new StringWriter())
            {
                SymbolPrinter.WriteFunctionTo(this, writer, onlyParams);
                return writer.ToString();
            }
        }

        private int? hashCode;
        public override int GetHashCode()
            => hashCode ??= HashCode.Combine(
                Name,
                Type,
                Parameters
                    .Aggregate(new HashCode(), (hash, param) => { hash.Add(param); return hash; })
                    .ToHashCode()
            );

        public override bool Equals(object? obj)
        {
            if (obj is FunctionSymbol other)
                return Name == other.Name &&
                    Type == other.Type &&
                    Parameters.SequenceEqual(other.Parameters);
            else
                return false;
        }
    }
}
