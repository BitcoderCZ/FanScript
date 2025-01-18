// <copyright file="BoundReturnStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundReturnStatement : BoundStatement
{
	public BoundReturnStatement(SyntaxNode syntax, BoundExpression? expression)
		: base(syntax)
	{
		Expression = expression;
	}

	public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

	public BoundExpression? Expression { get; }
}
