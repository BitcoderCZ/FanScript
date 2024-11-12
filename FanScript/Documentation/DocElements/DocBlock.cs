using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements;

public sealed class DocBlock : DocElement
{
    public DocBlock(ImmutableArray<DocElement> elements)
        : base([], null)
    {
        Elements = elements;
    }

    public ImmutableArray<DocElement> Elements { get; }
}
