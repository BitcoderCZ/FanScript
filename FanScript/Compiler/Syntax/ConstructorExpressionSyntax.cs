﻿// <copyright file="ConstructorExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class ConstructorExpressionSyntax : ExpressionSyntax
{
	internal ConstructorExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken, SyntaxToken openParenthesisToken, ExpressionSyntax expressionX, SyntaxToken comma0Token, ExpressionSyntax expressionY, SyntaxToken comma1Token, ExpressionSyntax expressionZ, SyntaxToken closeParenthesisToken)
		: base(syntaxTree)
	{
		KeywordToken = keywordToken;
		OpenParenthesisToken = openParenthesisToken;
		ExpressionX = expressionX;
		Comma0Token = comma0Token;
		ExpressionY = expressionY;
		Comma1Token = comma1Token;
		ExpressionZ = expressionZ;
		CloseParenthesisToken = closeParenthesisToken;
	}

	public override SyntaxKind Kind => SyntaxKind.ConstructorExpression;

	public SyntaxToken KeywordToken { get; }

	public SyntaxToken OpenParenthesisToken { get; }

	public ExpressionSyntax ExpressionX { get; }

	public SyntaxToken Comma0Token { get; }

	public ExpressionSyntax ExpressionY { get; }

	public SyntaxToken Comma1Token { get; }

	public ExpressionSyntax ExpressionZ { get; }

	public SyntaxToken CloseParenthesisToken { get; }
}
