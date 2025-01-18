// <copyright file="PostfixExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class PostfixExpressionSyntax : ExpressionSyntax
{
	internal PostfixExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken operatorToken)
		: base(syntaxTree)
	{
		IdentifierToken = identifierToken;
		OperatorToken = operatorToken;
	}

	public override SyntaxKind Kind => SyntaxKind.PostfixExpression;

	public SyntaxToken IdentifierToken { get; }

	public SyntaxToken OperatorToken { get; }
}
