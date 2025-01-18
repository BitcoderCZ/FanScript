// <copyright file="StatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public abstract class StatementSyntax : SyntaxNode
{
	private protected StatementSyntax(SyntaxTree syntaxTree)
		: base(syntaxTree)
	{
	}
}
