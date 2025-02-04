﻿// <copyright file="BoundExpressionStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundExpressionStatement : BoundStatement
{
	public BoundExpressionStatement(SyntaxNode syntax, BoundExpression expression)
		: base(syntax)
	{
		Expression = expression;
	}

	public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

	public BoundExpression Expression { get; }
}
