namespace FanScript.Compiler.Text
{
    public struct TextLocation
    {
        public static readonly TextLocation None = new TextLocation(null, default);

        private int? _startLine;

        private int? _endLine;

        public TextLocation(SourceText? text, TextSpan span)
        {
            Text = text;
            Span = span;
        }

        public SourceText? Text { get; }

        public TextSpan Span { get; }

        public readonly string FileName => Text!.FileName;

        public int StartLine => _startLine ??= Text!.GetLineIndex(Span.Start);

        public int StartCharacter => Span.Start - Text!.Lines[StartLine].Start;

        public int EndLine => _endLine ??= Text!.GetLineIndex(Span.End);

        public int EndCharacter => Span.End - Text!.Lines[EndLine].Start;

        public override string ToString()
            => $"{StartLine},{StartCharacter}..{EndLine},{EndCharacter} ({Span})";
    }
}
