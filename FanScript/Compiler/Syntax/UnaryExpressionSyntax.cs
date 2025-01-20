// <copyright file="UnaryExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class UnaryExpressionSyntax : ExpressionSyntax
{
	internal UnaryExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, ExpressionSyntax operand)
		: base(syntaxTree)
	{
		OperatorToken = operatorToken;
		Operand = operand;
	}

	public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

	public SyntaxToken OperatorToken { get; }

	public ExpressionSyntax Operand { get; }
}
