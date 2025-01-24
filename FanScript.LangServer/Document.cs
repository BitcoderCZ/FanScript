// <copyright file="Document.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer;

internal sealed class Document
{
	public readonly DocumentUri Uri;

	private readonly Lock _lock = new Lock();

	private string? _content;

	private int _treeVersion = -2;

	private SyntaxTree? _tree;

	private int _compilationVersion = -3;
	private Compilation? _compilation;

	public Document(DocumentUri uri)
	{
		Uri = uri;
	}

	public int ContentVersion { get; private set; } = -1;

	/// <summary>
	/// Gets a <see cref="SyntaxTree"/> for the current content.
	/// </summary>
	/// <remarks>
	/// Make sure to make a local variable, might change between accesses.
	/// </remarks>
	/// <value><see cref="SyntaxTree"/> for the current content.</value>
	public SyntaxTree? Tree
	{
		get
		{
			lock (_lock)
			{
				if (_treeVersion == ContentVersion)
				{
					return _tree;
				}

				_treeVersion = ContentVersion;
				return string.IsNullOrEmpty(_content)
					? null
					: (_tree = SyntaxTree.Parse(SourceText.From(_content, DocumentUri.GetFileSystemPath(Uri) ?? string.Empty)));
			}
		}
	}

	/// <summary>
	/// Gets a <see cref="Compiler.Compilation"/> for the current content.
	/// </summary>
	/// <remarks>
	/// Make sure to make a local variable, might change between accesses.
	/// </remarks>
	/// <value><see cref="Compiler.Compilation"/> for the current content.</value>
	public Compilation? Compilation
	{
		get
		{
			lock (_lock)
			{
				if (_compilationVersion == _treeVersion && _treeVersion == ContentVersion)
				{
					return _compilation;
				}

				SyntaxTree? tree = _tree;
				_compilationVersion = _treeVersion;
				return tree is null ? null : (_compilation = Compilation.Create(null, tree));
			}
		}
	}

	public void SetContent(string content, int? version)
	{
		lock (_lock)
		{
			_content = content;
			ContentVersion = version ?? ContentVersion + 1;
		}
	}

	public async Task<string?> GetContentAsync(CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			if (!string.IsNullOrEmpty(_content))
			{
				return _content;
			}
		}

		string? fileContent = null;
		try
		{
			string? path = DocumentUri.GetFileSystemPath(Uri);
			if (!string.IsNullOrEmpty(path))
			{
				fileContent = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
			}
		}
		catch
		{
		}

		lock (_lock)
		{
			if (!string.IsNullOrEmpty(fileContent))
			{
				_content = fileContent;
				ContentVersion++;
			}

			return _content;
		}
	}
}
