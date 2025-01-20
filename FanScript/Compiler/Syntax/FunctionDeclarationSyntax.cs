// <copyright file="FunctionDeclarationSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class FunctionDeclarationSyntax : MemberSyntax
{
	internal FunctionDeclarationSyntax(SyntaxTree syntaxTree, ModifierClauseSyntax modifiers, SyntaxToken keyword, TypeClauseSyntax? typeClause, SyntaxToken identifier, SyntaxToken openParenthesisToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParenthesisToken, BlockStatementSyntax body)
		: base(syntaxTree)
	{
		Modifiers = modifiers;
		Keyword = keyword;
		TypeClause = typeClause;
		Identifier = identifier;
		OpenParenthesisToken = openParenthesisToken;
		Parameters = parameters;
		CloseParenthesisToken = closeParenthesisToken;
		Body = body;
	}

	public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

	public ModifierClauseSyntax Modifiers { get; }

	public SyntaxToken Keyword { get; }

	public TypeClauseSyntax? TypeClause { get; }

	public SyntaxToken Identifier { get; }

	public SyntaxToken OpenParenthesisToken { get; }

	public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }

	public SyntaxToken CloseParenthesisToken { get; }

	public BlockStatementSyntax Body { get; }
}
