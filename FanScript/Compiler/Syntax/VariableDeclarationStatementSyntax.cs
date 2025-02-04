﻿// <copyright file="VariableDeclarationStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class VariableDeclarationStatementSyntax : StatementSyntax
{
	internal VariableDeclarationStatementSyntax(SyntaxTree syntaxTree, ModifierClauseSyntax modifierClause, TypeClauseSyntax typeClause, SyntaxToken identifierToken, StatementSyntax? optionalAssignment)
		: base(syntaxTree)
	{
		ModifierClause = modifierClause;
		TypeClause = typeClause;
		IdentifierToken = identifierToken;
		OptionalAssignment = optionalAssignment;
	}

	public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

	public ModifierClauseSyntax ModifierClause { get; }

	public TypeClauseSyntax TypeClause { get; }

	public SyntaxToken IdentifierToken { get; }

	public StatementSyntax? OptionalAssignment { get; }

	public override IEnumerable<SyntaxNode> GetChildren()
	{
		foreach (SyntaxNode modifier in ModifierClause.Modifiers)
		{
			yield return modifier;
		}

		yield return TypeClause;

		yield return OptionalAssignment is null ? IdentifierToken : OptionalAssignment;
	}
}
