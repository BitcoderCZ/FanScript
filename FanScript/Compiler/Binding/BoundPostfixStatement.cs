﻿// <copyright file="BoundPostfixStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundPostfixStatement : BoundStatement
{
	public BoundPostfixStatement(SyntaxNode syntax, VariableSymbol variable, PostfixKind postfixKind)
		: base(syntax)
	{
		Variable = variable;
		PostfixKind = postfixKind;
	}

	public override BoundNodeKind Kind => BoundNodeKind.PostfixStatement;

	public VariableSymbol Variable { get; }

	public PostfixKind PostfixKind { get; }
}
