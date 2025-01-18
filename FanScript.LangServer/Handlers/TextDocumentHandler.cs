using FanScript.Compiler;
using FanScript.LangServer.Utils;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace FanScript.LangServer.Handlers;

internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
	private readonly ILanguageServerFacade facade;
	private readonly ILogger<TextDocumentHandler> logger;
	private readonly ILanguageServerConfiguration configuration;

	private readonly Dictionary<DocumentUri, Document> documentCache = [];

	private readonly ConcurrentDictionary<DocumentUri, DelayedRunner> findErrorsDict = new();

	private readonly TextDocumentSelector textDocumentSelector = new TextDocumentSelector(
		new TextDocumentFilter
		{
			Pattern = "**/*.fcs"
		}
	);

	public TextDocumentHandler(ILanguageServerFacade facade, ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration)
	{
		this.facade = facade;
		this.logger = logger;
		this.configuration = configuration;
	}

	// TODO: use Incremental, and make textCache use StringBuilder or wrapper over List<char/byte> (implement ToString()!!!)
	public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

	public override Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
	{
		logger.LogInformation("Changed file: " + notification.TextDocument.Uri);

		// if delete, start - first char deleted, RangeLength - numb chars deleted
		TextDocumentContentChangeEvent? first = notification.ContentChanges.FirstOrDefault();
		if (Change == TextDocumentSyncKind.Full && first is not null)
			documentCache[notification.TextDocument.Uri].SetContent(first.Text, notification.TextDocument.Version);

		if (findErrorsDict.TryGetValue(notification.TextDocument.Uri, out var runner))
			runner.Invoke();

		return Unit.Task;
	}

	public override async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
	{
		await Task.Yield();

		var runner = new DelayedRunner(() => FindErrors(notification.TextDocument.Uri), TimeSpan.FromSeconds(0.75), TimeSpan.FromSeconds(4));
		findErrorsDict.AddOrUpdate(notification.TextDocument.Uri, runner, (uri, oldRunner) =>
		{
			oldRunner.Stop();
			return runner;
		});

		runner.Invoke();

		await configuration.GetScopedConfiguration(notification.TextDocument.Uri, token).ConfigureAwait(false);
		logger.LogInformation("Opened file: " + notification.TextDocument.Uri);

		return Unit.Value;
	}

	public override Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
	{
		if (configuration.TryGetScopedConfiguration(notification.TextDocument.Uri, out var disposable))
			disposable.Dispose();

		if (findErrorsDict.TryRemove(notification.TextDocument.Uri, out var runner))
			runner.Stop();

		return Unit.Task;
	}

	public override Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
		=> Unit.Task;

	protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSyncRegistrationOptions()
	{
		DocumentSelector = textDocumentSelector,
		Change = Change,
		Save = new SaveOptions() { IncludeText = true }
	};

	public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new TextDocumentAttributes(uri, "fanscript");

	public Document GetDocument(DocumentUri uri)
	{
		if (documentCache.TryGetValue(uri, out Document? document))
			return document;

		document = new Document(uri);
		documentCache.Add(uri, document);

		return document;
	}

	private void FindErrors(DocumentUri uri)
	{
		Document document = GetDocument(uri);
		Compilation? compilation = document.Compilation;

		if (compilation is null)
			return;

		var program = compilation.GetProgram();

		facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
		{
			Uri = uri,
			Version = document.ContentVersion,
			Diagnostics = new Container<Diagnostic>(convert(program.Diagnostics))
		});

		List<Diagnostic> convert(ImmutableArray<Compiler.Diagnostics.Diagnostic> diagnostics)
		{
			List<Diagnostic> result = [];

			foreach (var diagnostic in diagnostics)
				result.Add(new Diagnostic()
				{
					Severity = diagnostic.IsError ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
					Message = diagnostic.Message,
					Range = diagnostic.Location.ToRange()
				});

			return result;
		}
	}
}

internal class MyDocumentSymbolHandler : IDocumentSymbolHandler
{
	public async Task<SymbolInformationOrDocumentSymbolContainer?> Handle(
		DocumentSymbolParams request,
		CancellationToken cancellationToken
	)
	{
		// you would normally get this from a common source that is managed by current open editor, current active editor, etc.
		await Task.Yield();
		return null;

		//var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request), cancellationToken).ConfigureAwait(false);
		//var lines = content.Split('\n');
		//var symbols = new List<SymbolInformationOrDocumentSymbol>();
		//for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
		//{
		//    var line = lines[lineIndex];
		//    var parts = line.Split(' ', '.', '(', ')', '{', '}', '[', ']', ';');
		//    var currentCharacter = 0;
		//    foreach (var part in parts)
		//    {
		//        if (string.IsNullOrWhiteSpace(part))
		//        {
		//            currentCharacter += part.Length + 1;
		//            continue;
		//        }

		//        symbols.Add(
		//            new DocumentSymbol
		//            {
		//                Detail = part,
		//                Deprecated = true,
		//                Kind = SymbolKind.Field,
		//                Tags = new[] { SymbolTag.Deprecated },
		//                Range = new Range(
		//                    new Position(lineIndex, currentCharacter),
		//                    new Position(lineIndex, currentCharacter + part.Length)
		//                ),
		//                SelectionRange =
		//                    new Range(
		//                        new Position(lineIndex, currentCharacter),
		//                        new Position(lineIndex, currentCharacter + part.Length)
		//                    ),
		//                Name = part
		//            }
		//        );
		//        currentCharacter += part.Length + 1;
		//    }
		//}

		//// await Task.Delay(2000, cancellationToken);
		//return symbols;
	}

	public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) => new DocumentSymbolRegistrationOptions
	{
		DocumentSelector = TextDocumentSelector.ForLanguage("fanscript")
	};
}

internal class MyWorkspaceSymbolsHandler : IWorkspaceSymbolsHandler
{
	private readonly IServerWorkDoneManager serverWorkDoneManager;
	private readonly IProgressManager progressManager;
	private readonly ILogger<MyWorkspaceSymbolsHandler> logger;

	public MyWorkspaceSymbolsHandler(IServerWorkDoneManager serverWorkDoneManager, IProgressManager progressManager, ILogger<MyWorkspaceSymbolsHandler> logger)
	{
		this.serverWorkDoneManager = serverWorkDoneManager;
		this.progressManager = progressManager;
		this.logger = logger;
	}

	public async Task<Container<WorkspaceSymbol>?> Handle(
		WorkspaceSymbolParams request,
		CancellationToken cancellationToken
	)
	{
		//using var reporter = serverWorkDoneManager.For(
		//    request, new WorkDoneProgressBegin
		//    {
		//        Cancellable = true,
		//        Message = "This might take a while...",
		//        Title = "Some long task....",
		//        Percentage = 0
		//    }
		//);
		//using var partialResults = progressManager.For(request, cancellationToken);
		//if (partialResults != null)
		//{
		//    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);

		//    reporter.OnNext(
		//        new WorkDoneProgressReport
		//        {
		//            Cancellable = true,
		//            Percentage = 20
		//        }
		//    );
		//    await Task.Delay(500, cancellationToken).ConfigureAwait(false);

		//    reporter.OnNext(
		//        new WorkDoneProgressReport
		//        {
		//            Cancellable = true,
		//            Percentage = 40
		//        }
		//    );
		//    await Task.Delay(500, cancellationToken).ConfigureAwait(false);

		//    reporter.OnNext(
		//        new WorkDoneProgressReport
		//        {
		//            Cancellable = true,
		//            Percentage = 50
		//        }
		//    );
		//    await Task.Delay(500, cancellationToken).ConfigureAwait(false);

		//    partialResults.OnNext(
		//        new[] {
		//            new WorkspaceSymbol {
		//                ContainerName = "Partial Container",
		//                Kind = SymbolKind.Constant,
		//                Location = new Location {
		//                    Range = new Range(
		//                        new Position(2, 1),
		//                        new Position(2, 10)
		//                    )
		//                },
		//                Name = "Partial name"
		//            }
		//        }
		//    );

		//    reporter.OnNext(
		//        new WorkDoneProgressReport
		//        {
		//            Cancellable = true,
		//            Percentage = 70
		//        }
		//    );
		//    await Task.Delay(500, cancellationToken).ConfigureAwait(false);

		//    reporter.OnNext(
		//        new WorkDoneProgressReport
		//        {
		//            Cancellable = true,
		//            Percentage = 90
		//        }
		//    );

		//    partialResults.OnCompleted();
		//    return new WorkspaceSymbol[] { };
		//}

		await Task.Yield();

		try
		{
			return new[] {
				new WorkspaceSymbol {
					ContainerName = "Container",
					Kind = SymbolKind.Constant,
					Location = new Location {
						Range = new Range(
							new Position(1, 1),
							new Position(1, 10)
						)
					},
					Name = "name"
				}
			};
		}
		finally
		{
			//reporter.OnNext(
			//    new WorkDoneProgressReport
			//    {
			//        Cancellable = true,
			//        Percentage = 100
			//    }
			//);
		}
	}

	public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities) => new WorkspaceSymbolRegistrationOptions();
}
