﻿// <copyright file="LiteralExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class LiteralExpressionSyntax : ExpressionSyntax
{
	internal LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalToken)
		: this(syntaxTree, literalToken, literalToken.Value!)
	{
	}

	internal LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalToken, object value)
		: base(syntaxTree)
	{
		LiteralToken = literalToken;
		Value = value;
	}

	public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

	public SyntaxToken LiteralToken { get; }

	public object Value { get; }
}
