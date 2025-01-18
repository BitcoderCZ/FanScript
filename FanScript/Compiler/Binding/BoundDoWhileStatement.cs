// <copyright file="BoundDoWhileStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundDoWhileStatement : BoundLoopStatement
{
	public BoundDoWhileStatement(SyntaxNode syntax, BoundStatement body, BoundExpression condition, BoundLabel breakLabel, BoundLabel continueLabel)
		: base(syntax, breakLabel, continueLabel)
	{
		Body = body;
		Condition = condition;
	}

	public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

	public BoundStatement Body { get; }

	public BoundExpression Condition { get; }
}
