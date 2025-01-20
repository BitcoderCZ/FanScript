// <copyright file="CompilationUnitSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class CompilationUnitSyntax : SyntaxNode
{
	internal CompilationUnitSyntax(SyntaxTree syntaxTree, ImmutableArray<MemberSyntax> members, SyntaxToken endOfFileToken)
		: base(syntaxTree)
	{
		Members = members;
		EndOfFileToken = endOfFileToken;
	}

	public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

	public ImmutableArray<MemberSyntax> Members { get; }

	public SyntaxToken EndOfFileToken { get; }
}
