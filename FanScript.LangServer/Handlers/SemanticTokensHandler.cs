using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.LangServer.Classification;
using FanScript.LangServer.Utils;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace FanScript.LangServer.Handlers;

internal class SemanticTokensHandler : SemanticTokensHandlerBase
{
	private readonly ILanguageServerFacade facade;
	private readonly ILogger logger;

	private TextDocumentHandler? documentHandler;

	public SemanticTokensHandler(ILanguageServerFacade facade, ILogger<SemanticTokensHandler> logger)
	{
		this.facade = facade;
		this.logger = logger;
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
		documentHandler ??= facade.Workspace.GetService(typeof(TextDocumentHandler)) as TextDocumentHandler;

		if (documentHandler is null)
			return;

		Document document = documentHandler.GetDocument(identifier.TextDocument.Uri);
		string? content = await document.GetContentAsync(cancellationToken).ConfigureAwait(false);

		await Task.Yield();

		if (string.IsNullOrEmpty(content))
			return;

		try
		{
			SyntaxTree? tree = document.Tree;
			if (tree is null)
				return;

			TextSpan span = new TextSpan(0, int.MaxValue);
			if (identifier is SemanticTokensRangeParams rangeParams)
				span = rangeParams.Range.ToSpan(tree.Text);

			var nodes = Classifier.Classify(tree, span);
			foreach (var node in nodes)
			{
				SemanticTokenType? tokenType = node.Classification;

				TextLocation location = new TextLocation(tree.Text, node.Span);

				if (location.StartLine == location.EndLine)
				{
					builder.Push(
						location.ToRange(),
						tokenType
					);
				}
				else
				{
					// first line
					builder.Push(
						new Range(location.StartLine, location.StartCharacter, location.StartLine, tree.Text.Lines[location.StartLine].Lenght - location.StartCharacter),
						tokenType
					);

					for (int i = location.StartLine + 1; i < location.EndLine; i++)
					{
						int lineLength = tree.Text.Lines[i].Lenght;
						if (lineLength != 0)
							builder.Push(
								new Range(i, 0, i, lineLength),
								tokenType
							);
					}

					// last line
					builder.Push(
						new Range(location.EndLine, 0, location.EndLine, location.EndCharacter),
						tokenType
					);
				}
			}

			return;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failed to tokenize file '{identifier.TextDocument.Uri}'");
		}
	}

	protected override Task<SemanticTokensDocument>
		GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
		=> Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));

	protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
		SemanticTokensCapability capability, ClientCapabilities clientCapabilities
	)
		=> new SemanticTokensRegistrationOptions
		{
			DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
			Legend = new SemanticTokensLegend
			{
				TokenModifiers = capability.TokenModifiers,
				TokenTypes = capability.TokenTypes
			},
			Full = new SemanticTokensCapabilityRequestFull
			{
				Delta = false
			},
			Range = true
		};
}
