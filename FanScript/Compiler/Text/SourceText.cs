using System.Collections.Immutable;

namespace FanScript.Compiler.Text
{
    public sealed class SourceText
    {
        private readonly string text;

        private SourceText(string text, string fileName)
        {
            this.text = text;
            FileName = fileName;
            Lines = ParseLines(this, text);
        }

        public static SourceText From(string text, string fileName = "")
            => new SourceText(text, fileName);

        private static ImmutableArray<TextLine> ParseLines(SourceText sourceText, ReadOnlySpan<char> text)
        {
            ImmutableArray<TextLine>.Builder result = ImmutableArray.CreateBuilder<TextLine>();

            int position = 0;
            int lineStart = 0;

            while (position < text.Length)
            {
                int lineBreakWidth = GetLineBreakWidth(text, position);

                if (lineBreakWidth == 0)
                    position++;
                else
                {
                    AddLine(result, sourceText, position, lineStart, lineBreakWidth);

                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, sourceText, position, lineStart, 0);

            return result.ToImmutable();
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder result, SourceText sourceText, int position, int lineStart, int lineBreakWidth)
        {
            int lineLength = position - lineStart;
            int lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
            TextLine line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
            result.Add(line);
        }

        private static int GetLineBreakWidth(ReadOnlySpan<char> text, int position)
        {
            int c = text[position];
            char l = position + 1 >= text.Length ? '\0' : text[position + 1];

            if (c == '\r' && l == '\n')
                return 2;

            if (c == '\r' || c == '\n')
                return 1;

            return 0;
        }

        public ImmutableArray<TextLine> Lines { get; }

        public char this[int index] => text[index];

        public int Length => text.Length;

        public string FileName { get; }

        public int GetLineIndex(int position)
        {
            int lower = 0;
            int upper = Lines.Length - 1;

            while (lower <= upper)
            {
                int index = lower + (upper - lower) / 2;
                int start = Lines[index].Start;

                if (position == start)
                    return index;

                if (start > position)
                    upper = index - 1;
                else
                    lower = index + 1;
            }

            return lower - 1;
        }

        public override string ToString() => text;

        public string ToString(int start, int length) => text.Substring(start, length);

        public string ToString(TextSpan span) => ToString(span.Start, span.Length);
    }
}
