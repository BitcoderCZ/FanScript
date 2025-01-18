// <copyright file="BoundBlockStatement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding;

internal sealed class BoundBlockStatement : BoundStatement
{
	public BoundBlockStatement(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
		: base(syntax)
	{
		Statements = statements;
	}

	public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

	public ImmutableArray<BoundStatement> Statements { get; }

	public static BoundBlockStatement Create(BoundStatement statement)
		=> statement is BoundBlockStatement block ? block : new BoundBlockStatement(statement.Syntax, [statement]);
}
