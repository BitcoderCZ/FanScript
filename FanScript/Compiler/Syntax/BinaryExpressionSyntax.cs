﻿// <copyright file="BinaryExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class BinaryExpressionSyntax : ExpressionSyntax
{
	internal BinaryExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
		: base(syntaxTree)
	{
		Left = left;
		OperatorToken = operatorToken;
		Right = right;
	}

	public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

	public ExpressionSyntax Left { get; }

	public SyntaxToken OperatorToken { get; }

	public ExpressionSyntax Right { get; }
}
