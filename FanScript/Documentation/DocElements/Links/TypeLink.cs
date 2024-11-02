using System.Collections.Immutable;
using FanScript.Compiler.Symbols;
using FanScript.Utils;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class TypeLink : DocLink
    {
        public TypeLink(ImmutableArray<DocArg> arguments, DocString value, TypeSymbol type)
            : base(arguments, value)
        {
            Type = type;
        }

        public TypeSymbol Type { get; }

        public override (string DisplayString, string LinkString) GetStrings()
            => (Type.Name, Type.Name.ToUpperFirst());
    }
}
