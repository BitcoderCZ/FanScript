namespace FanScript.Compiler.Text
{
    public struct TextLocation
    {
        public static readonly TextLocation None = new TextLocation(SourceText.From(""), new TextSpan(0, 0));

        public TextLocation(SourceText text, TextSpan span)
        {
            Text = text;
            Span = span;
        }

        public SourceText Text { get; }
        public TextSpan Span { get; }

        public string FileName => Text.FileName;
        public int StartLine => Text.GetLineIndex(Span.Start);
        public int StartCharacter => Span.Start - Text.Lines[StartLine].Start;
        public int EndLine => Text.GetLineIndex(Span.End);
        public int EndCharacter => Span.End - Text.Lines[EndLine].Start;
    }
}
