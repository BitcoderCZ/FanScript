﻿// <copyright file="WhileStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class WhileStatementSyntax : StatementSyntax
{
	internal WhileStatementSyntax(SyntaxTree syntaxTree, SyntaxToken whileKeyword, ExpressionSyntax condition, StatementSyntax body)
		: base(syntaxTree)
	{
		WhileKeyword = whileKeyword;
		Condition = condition;
		Body = body;
	}

	public override SyntaxKind Kind => SyntaxKind.WhileStatement;

	public SyntaxToken WhileKeyword { get; }

	public ExpressionSyntax Condition { get; }

	public StatementSyntax Body { get; }
}
