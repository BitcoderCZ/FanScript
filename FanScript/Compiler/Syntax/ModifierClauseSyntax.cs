// <copyright file="ModifierClauseSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax;

public sealed partial class ModifierClauseSyntax : SyntaxNode
{
	public ModifierClauseSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers)
		: base(syntaxTree)
	{
		Modifiers = modifiers;
	}

	public override SyntaxKind Kind => SyntaxKind.ModifierClause;

	public ImmutableArray<SyntaxToken> Modifiers { get; }
}
