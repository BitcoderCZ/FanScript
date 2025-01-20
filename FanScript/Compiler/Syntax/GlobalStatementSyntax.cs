// <copyright file="GlobalStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class GlobalStatementSyntax : MemberSyntax
{
	internal GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement)
		: base(syntaxTree)
	{
		Statement = statement;
	}

	public override SyntaxKind Kind => SyntaxKind.GlobalStatement;

	public StatementSyntax Statement { get; }
}
