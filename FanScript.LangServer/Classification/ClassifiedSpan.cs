using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace FanScript.LangServer.Classification
{
    public readonly struct ClassifiedSpan
    {
        public readonly TextSpan Span;
        public readonly SemanticTokenType Classification;

        public ClassifiedSpan(TextSpan _span, SemanticTokenType _classification)
        {
            Span = _span;
            Classification = _classification;
        }
    }
}
