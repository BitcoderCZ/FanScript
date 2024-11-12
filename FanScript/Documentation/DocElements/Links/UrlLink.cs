using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements.Links;

public sealed class UrlLink : DocLink
{
    public UrlLink(ImmutableArray<DocArg> arguments, DocString value, string displayString, string url)
        : base(arguments, value)
    {
        DisplayString = displayString;
        Url = url;
    }

    public string DisplayString { get; }

    public string Url { get; }

    public override (string DisplayString, string LinkString) GetStrings()
        => (DisplayString, Url);
}
