using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocHeader : DocElement
    {
        public DocHeader(ImmutableArray<DocArg> arguments, DocElement value, int level)
            : base(arguments, value)
        {
            Value = value;
            Level = level;
        }

        public override DocElement Value { get; }

        public int Level { get; }
    }
}
