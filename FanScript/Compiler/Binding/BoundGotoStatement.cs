// <copyright file="BoundGotoStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal class BoundGotoStatement : BoundStatement
{
	public BoundGotoStatement(SyntaxNode syntax, BoundLabel label, bool isRollback)
		: base(syntax)
	{
		Label = label;
		IsRollback = isRollback;
	}

	public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

	public BoundLabel Label { get; }

	public bool IsRollback { get; }
}
