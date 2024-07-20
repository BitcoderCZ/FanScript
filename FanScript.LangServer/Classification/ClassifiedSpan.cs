using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.LangServer.Classification
{
    public readonly struct ClassifiedSpan
    {
        public readonly TextSpan Span;
        public readonly SemanticTokenType Classification;

        public ClassifiedSpan(TextSpan span, SemanticTokenType classification)
        {
            Span = span;
            Classification = classification;
        }
    }
}
