using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocCodeBlock : DocElement
    {
        public DocCodeBlock(ImmutableArray<DocArg> arguments, DocString value, string? lang)
            : base(arguments, value)
        {
            Value = value;
            Lang = lang;
        }

        public override DocString Value { get; }

        public string? Lang { get; }
    }
}
