using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocBlock : DocElement
    {
        public DocBlock(ImmutableArray<DocElement> elements)
            : base(ImmutableArray<DocArg>.Empty, null)
        {
            Elements = elements;
        }

        public ImmutableArray<DocElement> Elements { get; }
    }
}
