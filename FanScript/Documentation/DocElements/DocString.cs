using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocString : DocElement
    {
        public DocString(string text)
            : base(ImmutableArray<DocArg>.Empty, null)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
