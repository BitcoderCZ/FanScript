﻿// <copyright file="ReturnStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class ReturnStatementSyntax : StatementSyntax
{
	internal ReturnStatementSyntax(SyntaxTree syntaxTree, SyntaxToken returnKeyword, ExpressionSyntax? expression)
		: base(syntaxTree)
	{
		ReturnKeyword = returnKeyword;
		Expression = expression;
	}

	public override SyntaxKind Kind => SyntaxKind.ReturnStatement;

	public SyntaxToken ReturnKeyword { get; }

	public ExpressionSyntax? Expression { get; }
}
