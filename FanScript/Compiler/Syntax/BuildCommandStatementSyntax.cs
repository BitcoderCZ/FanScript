// <copyright file="BuildCommandStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class BuildCommandStatementSyntax : StatementSyntax
{
	public BuildCommandStatementSyntax(SyntaxTree syntaxTree, SyntaxToken hashtagToken, SyntaxToken identifier)
		: base(syntaxTree)
	{
		HashtagToken = hashtagToken;
		Identifier = identifier;
	}

	public override SyntaxKind Kind => SyntaxKind.BuildCommandStatement;

	public SyntaxToken HashtagToken { get; }

	public SyntaxToken Identifier { get; }
}
