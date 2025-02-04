﻿// <copyright file="BoundLoopStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal abstract class BoundLoopStatement : BoundStatement
{
	protected BoundLoopStatement(SyntaxNode syntax, BoundLabel breakLabel, BoundLabel continueLabel)
		: base(syntax)
	{
		BreakLabel = breakLabel;
		ContinueLabel = continueLabel;
	}

	public BoundLabel BreakLabel { get; }

	public BoundLabel ContinueLabel { get; }
}
