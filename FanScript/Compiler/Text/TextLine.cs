namespace FanScript.Compiler.Text
{
    public sealed class TextLine
    {
        public readonly SourceText Text;
        public readonly int Start;
        public readonly int Lenght;
        public readonly int LenghtIncludingLineBreak;

        public TextLine(SourceText text, int start, int lenght, int lenghtIncludingLineBreak)
        {
            Text = text;
            Start = start;
            Lenght = lenght;
            LenghtIncludingLineBreak = lenghtIncludingLineBreak;
        }

        public int End => Start + Lenght;

        public TextSpan Span  => new TextSpan(Start, Lenght);

        public TextSpan SpanIncludingLineBreak => new TextSpan(Start, LenghtIncludingLineBreak);

        public override string ToString()
            => Text.ToString(Span);
    }
}
