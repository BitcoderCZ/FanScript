using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers
{
    internal class HoverHandler : HoverHandlerBase
    {
        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
            => new HoverRegistrationOptions()
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
            };
    }
}
