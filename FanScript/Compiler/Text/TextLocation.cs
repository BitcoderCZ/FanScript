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
        private int? startLine;
        public int StartLine => startLine ??= Text.GetLineIndex(Span.Start);
        public int StartCharacter => Span.Start - Text.Lines[StartLine].Start;
        private int? endLine;
        public int EndLine => endLine ??= Text.GetLineIndex(Span.End);
        public int EndCharacter => Span.End - Text.Lines[EndLine].Start;

        public override string ToString()
            => $"{StartLine},{StartCharacter}..{EndLine},{EndCharacter} ({Span})";
    }
}
