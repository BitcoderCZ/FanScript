// <copyright file="ElseClauseSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class ElseClauseSyntax : SyntaxNode
{
	internal ElseClauseSyntax(SyntaxTree syntaxTree, SyntaxToken elseKeyword, StatementSyntax elseStatement)
		: base(syntaxTree)
	{
		ElseKeyword = elseKeyword;
		ElseStatement = elseStatement;
	}

	public override SyntaxKind Kind => SyntaxKind.ElseClause;

	public SyntaxToken ElseKeyword { get; }

	public StatementSyntax ElseStatement { get; }
}
