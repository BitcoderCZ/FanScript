// <copyright file="BoundNopStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundNopStatement : BoundStatement
{
	public BoundNopStatement(SyntaxNode syntax)
		: base(syntax)
	{
	}

	public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
}
