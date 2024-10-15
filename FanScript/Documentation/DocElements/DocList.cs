using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocList : DocElement
    {
        public DocList(ImmutableArray<DocArg> arguments, DocElement? value)
            : base(arguments, value)
        {
        }
    }

    public sealed class DocListItem : DocElement
    {
        public DocListItem(ImmutableArray<DocArg> arguments, DocElement? value)
            : base(arguments, value)
        {
        }
    }
}
