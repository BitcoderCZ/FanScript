// <copyright file="BreakStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
internal sealed partial class BreakStatementSyntax : StatementSyntax
{
	internal BreakStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
		: base(syntaxTree)
	{
		Keyword = keyword;
	}

	public override SyntaxKind Kind => SyntaxKind.BreakStatement;

	public SyntaxToken Keyword { get; }
}
