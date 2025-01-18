// <copyright file="BoundTreeVariableRenamer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Diagnostics;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters;

/// <summary>
/// Renames all variables, makes sure variables with the same name in differend scopes don't "collide"
/// </summary>
internal sealed class BoundTreeVariableRenamer : BoundTreeRewriter
{
	private readonly FunctionSymbol _function;
	private readonly Dictionary<VariableSymbol, VariableSymbol> _renamedDict = [];
	private Counter _varCount = new Counter(0);

	public BoundTreeVariableRenamer(FunctionSymbol function, Continuation? continuation = null)
	{
		_function = function;

		if (continuation is not null)
		{
			_varCount = continuation.Value.LastCount;
		}
	}

	public static BoundBlockStatement RenameVariables(BoundBlockStatement statement, FunctionSymbol function, ref Continuation? continuation)
	{
		BoundTreeVariableRenamer renamer = new BoundTreeVariableRenamer(function, continuation);
		BoundStatement res = renamer.RewriteBlockStatement(statement);

		continuation = new Continuation(renamer._varCount);

		return res is BoundBlockStatement blockRes ? blockRes : new BoundBlockStatement(statement.Syntax, [statement]);
	}

	protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
		=> node.Variable is BasicVariableSymbol
			? Assignment(node.Syntax, GetRenamedVar(node.Variable), RewriteExpression(node.Expression))
			: base.RewriteAssignmentStatement(node);

	protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
		=> node.Variable is BasicVariableSymbol
			? Variable(node.Syntax, GetRenamedVar(node.Variable))
			: base.RewriteVariableExpression(node);

	private VariableSymbol GetRenamedVar(VariableSymbol variable)
	{
		if (variable.IsGlobal || variable is ParameterSymbol)
		{
			return variable;
		}
		else if (_renamedDict.TryGetValue(variable, out var renamed))
		{
			return renamed;
		}
		else
		{
			if (variable is ParameterSymbol param)
			{
				int paramIndex = _function.Parameters.IndexOf(param);

				Debug.Assert(paramIndex >= 0, $"A {nameof(ParameterSymbol)} must be a parameter of {nameof(_function)}.");

				renamed = ReservedCompilerVariableSymbol.CreateParam(_function, paramIndex);
			}
			else
			{
				renamed = new CompilerVariableSymbol(_varCount.ToString(), variable.Modifiers, variable.Type);
				_varCount++;
			}

			_renamedDict.Add(variable, renamed);

			return renamed;
		}
	}

	public readonly struct Continuation
	{
		public readonly Counter LastCount;

		public Continuation(Counter lastCount)
		{
			LastCount = lastCount;
		}
	}
}
