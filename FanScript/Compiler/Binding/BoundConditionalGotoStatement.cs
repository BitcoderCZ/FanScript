﻿// <copyright file="BoundConditionalGotoStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundConditionalGotoStatement : BoundStatement
{
	public BoundConditionalGotoStatement(SyntaxNode syntax, BoundLabel label, BoundExpression condition, bool jumpIfTrue = true)
		: base(syntax)
	{
		Label = label;
		Condition = condition;
		JumpIfTrue = jumpIfTrue;
	}

	public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

	public BoundLabel Label { get; }

	public BoundExpression Condition { get; }

	public bool JumpIfTrue { get; }
}
