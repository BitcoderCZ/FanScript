// <copyright file="BoundEmitterHintStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundEmitterHintStatement : BoundStatement
{
	public BoundEmitterHintStatement(SyntaxNode syntax, HintKind hint)
		: base(syntax)
	{
		Hint = hint;
	}

	public enum HintKind
	{
		StatementBlockStart,
		StatementBlockEnd,
		HighlightStart,
		HighlightEnd,
	}

	public override BoundNodeKind Kind => BoundNodeKind.EmitterHintStatement;

	public HintKind Hint { get; }
}
