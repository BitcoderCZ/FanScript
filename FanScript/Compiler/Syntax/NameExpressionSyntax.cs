// <copyright file="NameExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class NameExpressionSyntax : AssignableExpressionSyntax
{
	internal NameExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken)
		: base(syntaxTree)
	{
		IdentifierToken = identifierToken;
	}

	public override SyntaxKind Kind => SyntaxKind.NameExpression;

	public SyntaxToken IdentifierToken { get; }
}
