// <copyright file="PrefixExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class PrefixExpressionSyntax : ExpressionSyntax
{
	internal PrefixExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, SyntaxToken identifierToken)
		: base(syntaxTree)
	{
		OperatorToken = operatorToken;
		IdentifierToken = identifierToken;
	}

	public override SyntaxKind Kind => SyntaxKind.PrefixExpression;

	public SyntaxToken OperatorToken { get; }

	public SyntaxToken IdentifierToken { get; }
}
