// <copyright file="ModifiersWExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public sealed partial class ModifiersWExpressionSyntax : SyntaxNode
{
	internal ModifiersWExpressionSyntax(SyntaxTree syntaxTree, ModifierClauseSyntax modifiers, ExpressionSyntax expression)
		: base(syntaxTree)
	{
		ModifierClause = modifiers;
		Expression = expression;
	}

	public override SyntaxKind Kind => SyntaxKind.ModifiersWExpressionSyntax;

	public ModifierClauseSyntax ModifierClause { get; }

	public ExpressionSyntax Expression { get; }
}
