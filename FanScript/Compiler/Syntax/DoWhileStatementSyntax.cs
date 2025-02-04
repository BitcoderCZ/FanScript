﻿// <copyright file="DoWhileStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class DoWhileStatementSyntax : StatementSyntax
{
	internal DoWhileStatementSyntax(SyntaxTree syntaxTree, SyntaxToken doKeyword, StatementSyntax body, SyntaxToken whileKeyword, ExpressionSyntax condition)
		: base(syntaxTree)
	{
		DoKeyword = doKeyword;
		Body = body;
		WhileKeyword = whileKeyword;
		Condition = condition;
	}

	public override SyntaxKind Kind => SyntaxKind.DoWhileStatement;

	public SyntaxToken DoKeyword { get; }

	public StatementSyntax Body { get; }

	public SyntaxToken WhileKeyword { get; }

	public ExpressionSyntax Condition { get; }
}
