using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.LangServer.Classification;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace FanScript.LangServer
{
#pragma warning disable 618
    public class SemanticTokensHandler : SemanticTokensHandlerBase
    {
        private readonly ILogger _logger;

        public SemanticTokensHandler(ILogger<SemanticTokensHandler> logger)
        {
            _logger = logger;
        }

        public override async Task<SemanticTokens?> Handle(
            SemanticTokensParams request, CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async Task<SemanticTokens?> Handle(
            SemanticTokensRangeParams request, CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async Task<SemanticTokensFullOrDelta?> Handle(
            SemanticTokensDeltaParams request,
            CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
            return result;
        }

        protected override async Task Tokenize(
            SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
            CancellationToken cancellationToken
        )
        {
            string path = DocumentUri.GetFileSystemPath(identifier)!;

            await Task.Yield();

            TextSpan span = new TextSpan(0, int.MaxValue);
            // store position start,end in span (line, character)
            //if (identifier is SemanticTokensRangeParams rangeParams)
            //    span = rangeParams.Range.Start;

            try
            {
                SyntaxTree tree = SyntaxTree.Load(path);
                var nodes = Classifier.Classify(tree, span);
                foreach (var node in nodes)
                {
                    SemanticTokenType? tokenType = node.Classification;

                    TextLocation location = new TextLocation(tree.Text, node.Span);

                    builder.Push(
                        new Range(location.StartLine, location.StartCharacter, location.EndLine, location.EndCharacter),
                        tokenType
                    );
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to tokenize file '{identifier.TextDocument.Uri}'");
            }

            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            await Task.Yield();

            using var typesEnumerator = RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
            using var modifiersEnumerator = RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();
            // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
            foreach (var (line, text) in content.Split('\n').Select((text, line) => (line, text)))
            {
                var parts = text.TrimEnd().Split(';', ' ', '.', '"', '(', ')');
                var index = 0;
                foreach (var part in parts)
                {
                    typesEnumerator.MoveNext();
                    modifiersEnumerator.MoveNext();
                    if (string.IsNullOrWhiteSpace(part)) continue;
                    index = text.IndexOf(part, index, StringComparison.Ordinal);
                    builder.Push(line, index, part.Length, typesEnumerator.Current, modifiersEnumerator.Current);
                }
            }
        }

        protected override Task<SemanticTokensDocument>
            GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
        }


        private IEnumerable<T> RotateEnum<T>(IEnumerable<T> values)
        {
            while (true)
            {
                foreach (var item in values)
                    yield return item;
            }
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
            SemanticTokensCapability capability, ClientCapabilities clientCapabilities
        )
        {
            return new SemanticTokensRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
                Legend = new SemanticTokensLegend
                {
                    TokenModifiers = capability.TokenModifiers,
                    TokenTypes = capability.TokenTypes
                },
                Full = new SemanticTokensCapabilityRequestFull
                {
                    Delta = true
                },
                Range = true
            };
        }
    }
#pragma warning restore 618
}
