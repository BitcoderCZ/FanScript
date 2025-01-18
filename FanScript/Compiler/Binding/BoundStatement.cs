// <copyright file="BoundStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal abstract class BoundStatement : BoundNode
{
	protected BoundStatement(SyntaxNode syntax)
		: base(syntax)
	{
	}
}
