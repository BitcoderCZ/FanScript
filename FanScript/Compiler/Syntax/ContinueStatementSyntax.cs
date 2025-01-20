﻿// <copyright file="ContinueStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
internal sealed partial class ContinueStatementSyntax : StatementSyntax
{
	internal ContinueStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
		: base(syntaxTree)
	{
		Keyword = keyword;
	}

	public override SyntaxKind Kind => SyntaxKind.ContinueStatement;

	public SyntaxToken Keyword { get; }
}
