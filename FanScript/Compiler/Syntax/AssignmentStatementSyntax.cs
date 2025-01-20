// <copyright file="AssignmentStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class AssignmentStatementSyntax : StatementSyntax
{
	internal AssignmentStatementSyntax(SyntaxTree syntaxTree, AssignableExpressionSyntax destination, SyntaxToken assignmentToken, ExpressionSyntax expression)
		: base(syntaxTree)
	{
		Destination = destination;
		AssignmentToken = assignmentToken;
		Expression = expression;
	}

	public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;

	public AssignableExpressionSyntax Destination { get; }

	public SyntaxToken AssignmentToken { get; }

	public ExpressionSyntax Expression { get; }
}
