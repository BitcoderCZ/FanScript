// <copyright file="BoundEventStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundEventStatement : BoundStatement
{
	public BoundEventStatement(SyntaxNode syntax, EventType type, BoundArgumentClause? argumentClause, BoundBlockStatement block)
		: base(syntax)
	{
		Type = type;
		ArgumentClause = argumentClause;
		Block = block;
	}

	public override BoundNodeKind Kind => BoundNodeKind.EventStatement;

	public EventType Type { get; }

	public BoundArgumentClause? ArgumentClause { get; }

	public BoundBlockStatement Block { get; }
}
