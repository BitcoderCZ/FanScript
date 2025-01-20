// <copyright file="ArgumentClauseSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

[GeneratedGetChildren]
public sealed partial class ArgumentClauseSyntax : SyntaxNode
{
	internal ArgumentClauseSyntax(SyntaxTree syntaxTree, SyntaxToken openParenthesisToken, SeparatedSyntaxList<ModifiersWExpressionSyntax> arguments, SyntaxToken closeParenthesisToken)
		: base(syntaxTree)
	{
		OpenParenthesisToken = openParenthesisToken;
		Arguments = arguments;
		CloseParenthesisToken = closeParenthesisToken;
	}

	public override SyntaxKind Kind => SyntaxKind.ArgumentClause;

	public SyntaxToken OpenParenthesisToken { get; }

	public SeparatedSyntaxList<ModifiersWExpressionSyntax> Arguments { get; }

	public SyntaxToken CloseParenthesisToken { get; }
}
