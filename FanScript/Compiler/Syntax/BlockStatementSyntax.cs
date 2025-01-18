// <copyright file="BlockStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax;

public sealed partial class BlockStatementSyntax : StatementSyntax
{
	internal BlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBraceToken)
		: base(syntaxTree)
	{
		OpenBraceToken = openBraceToken;
		Statements = statements;
		CloseBraceToken = closeBraceToken;
	}

	public override SyntaxKind Kind => SyntaxKind.BlockStatement;

	public SyntaxToken OpenBraceToken { get; }

	public ImmutableArray<StatementSyntax> Statements { get; }

	public SyntaxToken CloseBraceToken { get; }
}
