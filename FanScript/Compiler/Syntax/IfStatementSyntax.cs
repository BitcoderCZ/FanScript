// <copyright file="IfStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class IfStatementSyntax : StatementSyntax
{
	internal IfStatementSyntax(SyntaxTree syntaxTree, SyntaxToken ifKeyword, ExpressionSyntax condition, StatementSyntax thenStatement, ElseClauseSyntax? elseClause)
		: base(syntaxTree)
	{
		IfKeyword = ifKeyword;
		Condition = condition;
		ThenStatement = thenStatement;
		ElseClause = elseClause;
	}

	public override SyntaxKind Kind => SyntaxKind.IfStatement;

	public SyntaxToken IfKeyword { get; }

	public ExpressionSyntax Condition { get; }

	public StatementSyntax ThenStatement { get; }

	public ElseClauseSyntax? ElseClause { get; }
}
