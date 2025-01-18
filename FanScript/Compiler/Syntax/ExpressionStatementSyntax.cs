// <copyright file="ExpressionStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class ExpressionStatementSyntax : StatementSyntax
{
	internal ExpressionStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression)
		: base(syntaxTree)
	{
		Expression = expression;
	}

	public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

	public ExpressionSyntax Expression { get; }
}
