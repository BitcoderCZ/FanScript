namespace FanScript.Compiler.Text
{
    public struct TextSpan
    {
        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public static TextSpan FromBounds(int start, int end)
            => new TextSpan(start, end - start);

        public bool OverlapsWith(TextSpan span)
            => Start < span.End && End > span.Start;

        public static TextSpan operator +(TextSpan a, int b)
            => new TextSpan(a.Start + b, a.Length);
        public static TextSpan operator -(TextSpan a, int b)
            => new TextSpan(a.Start - b, a.Length);

        public override string ToString() => $"{Start}..{End}";
    }
}
