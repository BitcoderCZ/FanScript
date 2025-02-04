﻿// <copyright file="BoundArraySegmentExpression.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding;

internal sealed class BoundArraySegmentExpression : BoundExpression
{
	public BoundArraySegmentExpression(SyntaxNode syntax, TypeSymbol type, ImmutableArray<BoundExpression> elements)
		: base(syntax)
	{
		ElementType = type;
		Type = TypeSymbol.CreateGenericInstance(TypeSymbol.ArraySegment, type);
		Elements = elements;
	}

	public override BoundNodeKind Kind => BoundNodeKind.ArraySegmentExpression;

	public TypeSymbol ElementType { get; }

	public override TypeSymbol Type { get; }

	public ImmutableArray<BoundExpression> Elements { get; }
}
