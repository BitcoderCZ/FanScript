using FanScript.Compiler.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer
{
    internal class CompletionHandler : CompletionHandlerBase
    {
        private static readonly ImmutableArray<string> keywords = Enum.GetValues<SyntaxKind>()
                    .Where(kind => kind.IsKeyword())
                    .Select(kind => kind.GetText()!)
                    .ToImmutableArray();
        private static readonly ImmutableArray<string> modifiers = Enum.GetValues<SyntaxKind>()
                    .Where(kind => kind.IsModifier())
                    .Select(kind => kind.GetText()!)
                    .ToImmutableArray();

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            return new Task<CompletionItem>(() => request, cancellationToken);
        }

        public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            return new CompletionList(
                Enumerable.Concat(
                    keywords,
                    modifiers
                )
                .Select(text => new CompletionItem()
                {
                    Label = text,
                    Kind = CompletionItemKind.Keyword
                })
            );
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
            => new CompletionRegistrationOptions()
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
                CompletionItem = new CompletionRegistrationCompletionItemOptions()
                {
                    LabelDetailsSupport = true,
                }
            };
    }
}
