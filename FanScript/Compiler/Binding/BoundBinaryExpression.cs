﻿// <copyright file="BoundBinaryExpression.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundBinaryExpression : BoundExpression
{
	public BoundBinaryExpression(SyntaxNode syntax, BoundExpression left, BoundBinaryOperator op, BoundExpression right)
		: base(syntax)
	{
		Left = left;
		Op = op;
		Right = right;
		ConstantValue = ConstantFolding.Fold(left, op, right);
	}

	public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;

	public override TypeSymbol Type => Op.Type;

	public BoundExpression Left { get; }

	public BoundBinaryOperator Op { get; }

	public BoundExpression Right { get; }

	public override BoundConstant? ConstantValue { get; }
}
