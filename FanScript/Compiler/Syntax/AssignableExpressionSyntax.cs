// <copyright file="AssignableExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public abstract class AssignableExpressionSyntax : ExpressionSyntax
{
	private protected AssignableExpressionSyntax(SyntaxTree syntaxTree)
		: base(syntaxTree)
	{
	}
}
