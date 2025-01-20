// <copyright file="PropertyExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class PropertyExpressionSyntax : AssignableExpressionSyntax
{
	internal PropertyExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax baseExpression, SyntaxToken dotToken, ExpressionSyntax expression)
		: base(syntaxTree)
	{
		BaseExpression = baseExpression;
		DotToken = dotToken;
		Expression = expression;
	}

	public override SyntaxKind Kind => SyntaxKind.PropertyExpression;

	public ExpressionSyntax BaseExpression { get; }

	public SyntaxToken DotToken { get; }

	public ExpressionSyntax Expression { get; }
}
