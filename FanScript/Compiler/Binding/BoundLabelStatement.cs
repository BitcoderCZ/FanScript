// <copyright file="BoundLabelStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundLabelStatement : BoundStatement
{
	public BoundLabelStatement(SyntaxNode syntax, BoundLabel label)
		: base(syntax)
	{
		Label = label;
	}

	public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

	public BoundLabel Label { get; }
}
