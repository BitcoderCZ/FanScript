// <copyright file="PrefixStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class PrefixStatementSyntax : StatementSyntax
{
	internal PrefixStatementSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, SyntaxToken identifierToken)
		: base(syntaxTree)
	{
		OperatorToken = operatorToken;
		IdentifierToken = identifierToken;
	}

	public override SyntaxKind Kind => SyntaxKind.PrefixStatement;

	public SyntaxToken OperatorToken { get; }

	public SyntaxToken IdentifierToken { get; }
}
