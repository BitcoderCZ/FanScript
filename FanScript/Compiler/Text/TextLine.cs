using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Text
{
    public sealed class TextLine
    {
        public TextLine(SourceText text, int start, int lenght, int lenghtIncludingLineBreak)
        {
            Text = text;
            Start = start;
            Lenght = lenght;
            LenghtIncludingLineBreak = lenghtIncludingLineBreak;
        }

        public readonly SourceText Text;
        public readonly int Start;
        public readonly int Lenght;
        public int End 
            => Start + Lenght;
        public readonly int LenghtIncludingLineBreak;
        public TextSpan Span 
            => new TextSpan(Start, Lenght);
        public TextSpan SpanIncludingLineBreak
            => new TextSpan(Start, LenghtIncludingLineBreak);

        public override string ToString() 
            => Text.ToString(Span);
    }
}
