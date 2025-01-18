// <copyright file="MemberSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public abstract class MemberSyntax : SyntaxNode
{
	private protected MemberSyntax(SyntaxTree syntaxTree)
		: base(syntaxTree)
	{
	}
}
