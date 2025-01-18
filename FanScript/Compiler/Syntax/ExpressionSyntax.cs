// <copyright file="ExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public abstract class ExpressionSyntax : SyntaxNode
{
	private protected ExpressionSyntax(SyntaxTree syntaxTree)
		: base(syntaxTree)
	{
	}
}
