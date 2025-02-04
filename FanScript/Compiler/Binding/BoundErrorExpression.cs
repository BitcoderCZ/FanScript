﻿// <copyright file="BoundErrorExpression.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

// TODO: Should the error expression accept an array of bound nodes so that we don't drop
//       parts of the bound tree on the floor?
internal sealed class BoundErrorExpression : BoundExpression
{
	public BoundErrorExpression(SyntaxNode syntax)
		: base(syntax)
	{
	}

	public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;

	public override TypeSymbol Type => TypeSymbol.Error;
}
