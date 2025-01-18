// <copyright file="ParenthesizedExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class ParenthesizedExpressionSyntax : ExpressionSyntax
{
	internal ParenthesizedExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openParenthesisToken, ExpressionSyntax expression, SyntaxToken closeParenthesisToken)
		: base(syntaxTree)
	{
		OpenParenthesisToken = openParenthesisToken;
		Expression = expression;
		CloseParenthesisToken = closeParenthesisToken;
	}

	public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

	public SyntaxToken OpenParenthesisToken { get; }

	public ExpressionSyntax Expression { get; }

	public SyntaxToken CloseParenthesisToken { get; }
}
