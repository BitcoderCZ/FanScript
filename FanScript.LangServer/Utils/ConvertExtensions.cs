using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace FanScript.LangServer.Utils
{
    internal static class ConvertExtensions
    {
        public static Range ToRange(this TextLocation location)
            => new Range(location.StartLine, location.StartCharacter, location.EndLine, location.EndCharacter);

        public static TextSpan ToSpan(this Position position, SourceText text)
            => new TextSpan(
                text.Lines[position.Line].Start + position.Character,
                1
            );
        public static TextSpan ToSpan(this Range range, SourceText text)
            => TextSpan.FromBounds(
                text.Lines[range.Start.Line].Start + range.Start.Character,
                text.Lines[range.End.Line].Start + range.End.Character
            );
    }
}
