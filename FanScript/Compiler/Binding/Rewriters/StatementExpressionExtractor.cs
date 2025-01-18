// <copyright file="StatementExpressionExtractor.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters;

// sadly cannot use BoundTreeRewriter, because expressions need to return more data
// required for stuff like assignments, ++, -- and non void functions, because these can only be executed from a void (execution) wire, so they are "extracted" and ran before/after the expression
internal sealed class StatementExpressionExtractor
{
	private readonly State _state;

	public StatementExpressionExtractor(FunctionSymbol function)
	{
		_state = new State(function);
	}

	public static BoundStatement Extract(FunctionSymbol function, BoundStatement statement)
	{
		StatementExpressionExtractor extractor = new StatementExpressionExtractor(function);
		return extractor.RewriteStatement(statement);
	}

	private static BoundNopStatement RewriteNopStatement(BoundNopStatement node)
	  => node;

	private static BoundPostfixStatement RewritePostfixStatement(BoundPostfixStatement node)
		=> node;

	private static BoundPrefixStatement RewritePrefixStatement(BoundPrefixStatement node)
		=> node;

	private static BoundLabelStatement RewriteLabelStatement(BoundLabelStatement node)
		=> node;

	private static BoundGotoStatement RewriteGotoStatement(BoundGotoStatement node)
		=> node;

	private static BoundEmitterHintStatement RewriteEmitterHint(BoundEmitterHintStatement node)
		=> node;

	private static ExpressionResult RewriteErrorExpression(BoundErrorExpression node)
		=> new ExpressionResult(node);

	private static ExpressionResult RewriteLiteralExpression(BoundLiteralExpression node)
		=> new ExpressionResult(node);

	private static ExpressionResult RewritePostfixExpression(BoundPostfixExpression node)
		=> new ExpressionResult([], Variable(node.Syntax, node.Variable), [PostfixStatement(node.Syntax, node.Variable, node.PostfixKind)]);

	private static ExpressionResult RewritePrefixExpression(BoundPrefixExpression node)
		=> new ExpressionResult([PrefixStatement(node.Syntax, node.Variable, node.PrefixKind)], Variable(node.Syntax, node.Variable), []);

	private BoundStatement RewriteStatement(BoundStatement node)
	{
		_state.ResetVarCount();

		return node switch
		{
			BoundBlockStatement blockStatement => RewriteBlockStatement(blockStatement),
			BoundEventStatement eventStatement => RewriteEventStatement(eventStatement),
			BoundNopStatement nopStatement => RewriteNopStatement(nopStatement),
			BoundPostfixStatement postfixStatement => RewritePostfixStatement(postfixStatement),
			BoundPrefixStatement prefixStatement => RewritePrefixStatement(prefixStatement),
			BoundVariableDeclarationStatement variableDeclarationStatement => RewriteVariableDeclaration(variableDeclarationStatement),
			BoundAssignmentStatement assignmentStatement => RewriteAssignmentStatement(assignmentStatement),
			BoundCompoundAssignmentStatement compoundAssignmentStatement => RewriteCompoundAssignmentStatement(compoundAssignmentStatement),
			BoundIfStatement ifStatement => RewriteIfStatement(ifStatement),
			BoundLabelStatement labelStatement => RewriteLabelStatement(labelStatement),
			BoundGotoStatement gotoStatement => RewriteGotoStatement(gotoStatement),
			BoundEventGotoStatement eventGotoStatement => RewriteEventGotoStatement(eventGotoStatement),
			BoundConditionalGotoStatement conditionalGotoStatement => RewriteConditionalGotoStatement(conditionalGotoStatement),
			BoundReturnStatement returnStatement => RewriteReturnStatement(returnStatement),
			BoundEmitterHintStatement emitterHint => RewriteEmitterHint(emitterHint),
			BoundCallStatement callStatement => RewriteCallStatement(callStatement),
			BoundExpressionStatement expressionStatement => RewriteExpressionStatement(expressionStatement),

			_ => throw new UnexpectedBoundNodeException(node),
		};
	}

	private BoundBlockStatement RewriteBlockStatement(BoundBlockStatement node)
	{
		ImmutableArray<BoundStatement>.Builder? builder = null;

		for (int i = 0; i < node.Statements.Length; i++)
		{
			BoundStatement oldStatement = node.Statements[i];
			BoundStatement newStatement = RewriteStatement(oldStatement);
			if (newStatement != oldStatement)
			{
				if (builder is null)
				{
					builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);

					for (int j = 0; j < i; j++)
					{
						builder.Add(node.Statements[j]);
					}
				}
			}

			builder?.Add(newStatement);
		}

		return builder is null ? node : new BoundBlockStatement(node.Syntax, builder.DrainToImmutable());
	}

	private BoundStatement RewriteEventStatement(BoundEventStatement node)
	{
		var (before, argumentClause, after) = node.ArgumentClause is null ? ([], null, []) : RewriteArgumentClause(node.ArgumentClause);

		BoundBlockStatement block = RewriteBlockStatement(node.Block);

		return argumentClause == node.ArgumentClause && block == node.Block && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty
			? node
			: HandleBeforeAfter(before, new BoundEventStatement(node.Syntax, node.Type, argumentClause, block), after);
	}

	private BoundVariableDeclarationStatement RewriteVariableDeclaration(BoundVariableDeclarationStatement node)
	{
		if (node.OptionalAssignment is null)
		{
			return node;
		}

		BoundStatement assignment = RewriteStatement(node.OptionalAssignment);
		return assignment == node.OptionalAssignment ? node : new BoundVariableDeclarationStatement(node.Syntax, node.Variable, assignment);
	}

	private BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
	{
		ExpressionResult expression = RewriteExpression(node.Expression);
		return expression.IsSameAs(node.Expression)
			? node
			: HandleBeforeAfter(expression.Before, new BoundAssignmentStatement(node.Syntax, node.Variable, expression.Expression), expression.After);
	}

	private BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
	{
		ExpressionResult expression = RewriteExpression(node.Expression);
		return expression.IsSameAs(node.Expression)
			? node
			: HandleBeforeAfter(expression.Before, new BoundCompoundAssignmentStatement(node.Syntax, node.Variable, node.Op, expression.Expression), expression.After);
	}

	private BoundStatement RewriteIfStatement(BoundIfStatement node)
	{
		ExpressionResult condition = RewriteExpression(node.Condition);
		BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
		BoundStatement? elseStatement = node.ElseStatement is null ? null : RewriteStatement(node.ElseStatement);
		return condition.IsSameAs(node.Condition) && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement
			? node
			: !condition.Any
			? new BoundIfStatement(node.Syntax, condition.Expression, thenStatement, elseStatement)
			: HandleBeforeAfterWithTemp(condition, newEx => new BoundIfStatement(node.Syntax, newEx, thenStatement, elseStatement));
	}

	private BoundStatement RewriteEventGotoStatement(BoundEventGotoStatement node)
	{
		if (node.ArgumentClause is null)
		{
			return node;
		}

		var (before, clause) = RewriteArgumentClauseBeforeOnly(node.ArgumentClause);

		return clause == node.ArgumentClause
			? node
			: HandleBeforeAfter(
			before,
			new BoundEventGotoStatement(node.Syntax, node.Label, node.EventType, clause),
			[]);
	}

	private BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
	{
		ExpressionResult condition = RewriteExpression(node.Condition);
		return condition.IsSameAs(node.Condition)
			? node
			: !condition.Any
			? new BoundConditionalGotoStatement(node.Syntax, node.Label, condition.Expression, node.JumpIfTrue)
			: HandleBeforeAfterWithTemp(condition, newEx => new BoundConditionalGotoStatement(node.Syntax, node.Label, newEx, node.JumpIfTrue));
	}

	private BoundStatement RewriteReturnStatement(BoundReturnStatement node)
	{
		if (node.Expression is null)
		{
			return node;
		}

		ExpressionResult expression = RewriteExpression(node.Expression);
		return expression.IsSameAs(node.Expression)
			? node
			: !expression.Any
			? new BoundReturnStatement(node.Syntax, expression.Expression)
			: HandleBeforeAfterWithTemp(expression, newEx => new BoundReturnStatement(node.Syntax, newEx));
	}

	private BoundStatement RewriteCallStatement(BoundCallStatement node)
	{
		var (before, clause, after) = RewriteArgumentClause(node.ArgumentClause);

		return clause == node.ArgumentClause && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty
			? node
			: HandleBeforeAfter(before, new BoundCallStatement(node.Syntax, node.Function, clause, node.ReturnType, node.GenericType, node.ResultVariable), after);
	}

	private BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
	{
		ExpressionResult expression = RewriteExpression(node.Expression);
		return expression.IsSameAs(node.Expression)
			? node
			: HandleBeforeAfter(expression.Before, new BoundExpressionStatement(node.Syntax, expression.Expression), expression.After);
	}

	private ExpressionResult RewriteExpression(BoundExpression node)
		=> node switch
		{
			BoundErrorExpression errorExpression => RewriteErrorExpression(errorExpression),
			BoundLiteralExpression literalExpression => RewriteLiteralExpression(literalExpression),
			BoundVariableExpression variableExpression => RewriteVariableExpression(variableExpression),
			BoundUnaryExpression unaryExpression => RewriteUnaryExpression(unaryExpression),
			BoundBinaryExpression binaryExpression => RewriteBinaryExpression(binaryExpression),
			BoundCallExpression callExpression => RewriteCallExpression(callExpression),
			BoundConversionExpression conversionExpression => RewriteConversionExpression(conversionExpression),
			BoundConstructorExpression constructorExpression => RewriteConstructorExpression(constructorExpression),
			BoundPostfixExpression postfixExpression => RewritePostfixExpression(postfixExpression),
			BoundPrefixExpression prefixExpression => RewritePrefixExpression(prefixExpression),
			BoundArraySegmentExpression arraySegmentExpression => RewriteArraySegmentExpression(arraySegmentExpression),
			BoundAssignmentExpression assignmentExpression => RewriteAssignmentExpression(assignmentExpression),

			_ => throw new UnexpectedBoundNodeException(node),
		};

	private ExpressionResult RewriteVariableExpression(BoundVariableExpression node)
	{
		switch (node.Variable)
		{
			case PropertySymbol prop:
				{
					ExpressionResult newEx = RewriteExpression(prop.Expression);

					return newEx.IsSameAs(prop.Expression)
						? new ExpressionResult(node)
						: ExpressionResult.Enclose(new BoundVariableExpression(node.Syntax, new PropertySymbol(prop.Definition, newEx.Expression)), newEx);
				}

			default:
				return new ExpressionResult(node);
		}
	}

	private ExpressionResult RewriteUnaryExpression(BoundUnaryExpression node)
	{
		ExpressionResult operand = RewriteExpression(node.Operand);

		return operand.IsSameAs(node.Operand)
			? new ExpressionResult(node)
			: ExpressionResult.Enclose(new BoundUnaryExpression(node.Syntax, node.Op, operand.Expression), operand);
	}

	private ExpressionResult RewriteBinaryExpression(BoundBinaryExpression node)
	{
		ExpressionResult left = RewriteExpression(node.Left);
		ExpressionResult right = RewriteExpression(node.Right);

		if (left.IsSameAs(node.Left) && right.IsSameAs(node.Right))
		{
			return new ExpressionResult(node);
		}

		var opKind = node.Op.Kind;
		if (right.Any && (opKind == BoundBinaryOperatorKind.LogicalAnd || opKind == BoundBinaryOperatorKind.LogicalOr))
		{
			// Short-circuit evaluation
			var variable = _state.GetTempVar(left.Expression.Type);
			var varEx = Variable(left.Expression.Syntax, variable);

			var before = ImmutableArray.CreateBuilder<BoundStatement>(left.Length + 1 + right.Length);
			ImmutableArray<BoundStatement> after = right.After;

			before.AddRangeSafe(left.Before);
			before.Add(Assignment(left.Expression.Syntax, variable, left.Expression));
			before.AddRangeSafe(left.After);

			BoundLabel label = _state.GetLabel("short_circuit");

			if (opKind == BoundBinaryOperatorKind.LogicalAnd)
			{
				before.Add(GotoFalse(left.Expression.Syntax, label, varEx));
			}
			else
			{
				before.Add(GotoTrue(left.Expression.Syntax, label, varEx));
			}

			before.AddRangeSafe(right.Before);
			before.Add(Assignment(right.Expression.Syntax, variable, right.Expression));
			before.AddRangeSafe(right.After);
			before.Add(Label(right.Expression.Syntax, label));

			return new ExpressionResult(before.DrainToImmutable(), varEx, after);
		}
		else
		{
			var (before, after, leftEx, rightEx) = ExpressionResult.Resolve(_state, left, right);

			return new ExpressionResult(before, new BoundBinaryExpression(node.Syntax, leftEx, node.Op, rightEx), after);
		}
	}

	private ExpressionResult RewriteCallExpression(BoundCallExpression node)
	{
		var (before, argumentClause, after) = RewriteArgumentClause(node.ArgumentClause);

		bool extract = node.Function.Type != TypeSymbol.Void && node.Function is not BuiltinFunctionSymbol;

		if (argumentClause == node.ArgumentClause && before.IsDefaultOrEmpty && after.IsDefaultOrEmpty &&
			!extract)
		{
			return new ExpressionResult(node);
		}

		if (extract)
		{
			var temp = _state.GetTempVar(node.Function.Type, true);

			return new ExpressionResult(
				(before.IsDefault ? Enumerable.Empty<BoundStatement>() : before).Concat([
					new BoundCallStatement(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType, temp)
				]).ToImmutableArray(),
				Variable(node.Syntax, temp),
				after);
		}
		else
		{
			return new ExpressionResult(before, new BoundCallExpression(node.Syntax, node.Function, argumentClause, node.ReturnType, node.GenericType), after);
		}
	}

	private ExpressionResult RewriteConversionExpression(BoundConversionExpression node)
	{
		ExpressionResult expression = RewriteExpression(node.Expression);

		return expression.IsSameAs(node.Expression)
			? new ExpressionResult(node)
			: ExpressionResult.Enclose(new BoundConversionExpression(node.Syntax, node.Type, expression.Expression), expression);
	}

	private ExpressionResult RewriteConstructorExpression(BoundConstructorExpression node)
	{
		ExpressionResult exX = RewriteExpression(node.ExpressionX);
		ExpressionResult exY = RewriteExpression(node.ExpressionY);
		ExpressionResult exZ = RewriteExpression(node.ExpressionZ);

		if (exX.IsSameAs(node.ExpressionX) && exY.IsSameAs(node.ExpressionY) && exZ.IsSameAs(node.ExpressionZ))
		{
			return new ExpressionResult(node);
		}

		var (before, after, expressions) = ExpressionResult.Resolve(_state, [exX, exY, exZ]);

		return new ExpressionResult(before, new BoundConstructorExpression(node.Syntax, node.Type, expressions[0], expressions[1], expressions[2]), after);
	}

	private ExpressionResult RewriteArraySegmentExpression(BoundArraySegmentExpression node)
	{
		ImmutableArray<ExpressionResult>.Builder? builder = null;

		for (int i = 0; i < node.Elements.Length; i++)
		{
			BoundExpression oldElement = node.Elements[i];
			ExpressionResult newElement = RewriteExpression(oldElement);
			if (!newElement.IsSameAs(oldElement))
			{
				if (builder is null)
				{
					builder = ImmutableArray.CreateBuilder<ExpressionResult>(node.Elements.Length);

					for (int j = 0; j < i; j++)
					{
						builder.Add(new ExpressionResult(node.Elements[j]));
					}
				}
			}

			builder?.Add(newElement);
		}

		if (builder is null)
		{
			return new ExpressionResult(node);
		}

		var (before, after, expressions) = ExpressionResult.Resolve(_state, builder.DrainToImmutable().AsSpan());

		return new ExpressionResult(before, new BoundArraySegmentExpression(node.Syntax, node.ElementType, [.. expressions]), after);
	}

	private ExpressionResult RewriteAssignmentExpression(BoundAssignmentExpression node)
	{
		ExpressionResult expression = RewriteExpression(node.Expression);

		return new ExpressionResult(
			(expression.Before.IsDefault ? Enumerable.Empty<BoundStatement>() : expression.Before).Concat([
				Assignment(node.Syntax, node.Variable, expression.Expression)
			]).ToImmutableArray(),
			Variable(node.Syntax, node.Variable),
			expression.After);
	}

	#region Helper functions
	private (ImmutableArray<BoundStatement> Before, BoundArgumentClause Clause, ImmutableArray<BoundStatement> After) RewriteArgumentClause(BoundArgumentClause node)
	{
		ImmutableArray<ExpressionResult>.Builder? builder = null;

		for (int i = 0; i < node.Arguments.Length; i++)
		{
			BoundExpression oldArgument = node.Arguments[i];
			ExpressionResult newArgument = RewriteExpression(oldArgument);
			if (!newArgument.IsSameAs(oldArgument))
			{
				if (builder is null)
				{
					builder = ImmutableArray.CreateBuilder<ExpressionResult>(node.Arguments.Length);

					for (int j = 0; j < i; j++)
					{
						builder.Add(new ExpressionResult(node.Arguments[j]));
					}
				}
			}

			builder?.Add(newArgument);
		}

		if (builder is null)
		{
			return ([], node, []);
		}

		var (before, after, expressions) = ExpressionResult.Resolve(_state, builder.DrainToImmutable().AsSpan());

		return (before, new BoundArgumentClause(node.Syntax, node.ArgModifiers, [.. expressions]), after);
	}

	private (ImmutableArray<BoundStatement> Before, BoundArgumentClause Clause) RewriteArgumentClauseBeforeOnly(BoundArgumentClause node)
	{
		ImmutableArray<ExpressionResult>.Builder? builder = null;

		for (int i = 0; i < node.Arguments.Length; i++)
		{
			BoundExpression oldArgument = node.Arguments[i];
			ExpressionResult newArgument = RewriteExpression(oldArgument);
			if (!newArgument.IsSameAs(oldArgument))
			{
				if (builder is null)
				{
					builder = ImmutableArray.CreateBuilder<ExpressionResult>(node.Arguments.Length);

					for (int j = 0; j < i; j++)
					{
						builder.Add(new ExpressionResult(node.Arguments[j]));
					}
				}
			}

			builder?.Add(newArgument);
		}

		if (builder is null)
		{
			return ([], node);
		}

		var (before, expressions) = ExpressionResult.ResolveBeforeOnly(_state, builder.DrainToImmutable().AsSpan());

		return (before, new BoundArgumentClause(node.Syntax, node.ArgModifiers, [.. expressions]));
	}

	private BoundStatement HandleBeforeAfterWithTemp(ExpressionResult result, Func<BoundExpression, BoundStatement> getStatement)
		=> HandleBeforeAfterWithTemp(result.Before, result.Expression, result.After, getStatement);

	private BoundStatement HandleBeforeAfterWithTemp(ImmutableArray<BoundStatement> before, BoundExpression expression, ImmutableArray<BoundStatement> after, Func<BoundExpression, BoundStatement> getStatement)
	{
		if (after.IsDefaultOrEmpty)
		{
			return HandleBeforeAfter(before, getStatement(expression), after); // don't need to worry about after, temp var, because no changes are made after the expression
		}

		VariableSymbol temp = _state.GetTempVar(expression.Type);

		BoundStatement statement = HandleBeforeAfter(before, Assignment(expression.Syntax, temp, expression), after);

		return Block(
			statement.Syntax,
			statement,
			getStatement(Variable(expression.Syntax, temp)));
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "whomp whomp")]
	private static BoundStatement HandleBeforeAfter(ImmutableArray<BoundStatement> before, BoundStatement statement, ImmutableArray<BoundStatement> after)
	{
		if (before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)
		{
			return statement;
		}
		else if (before.IsDefaultOrEmpty)
		{
			BoundStatement[] statements = new BoundStatement[after.Length + 1];
			statements[0] = statement;
			after.CopyTo(statements, 1);
			return Block(statement.Syntax, statements);
		}
		else if (after.IsDefaultOrEmpty)
		{
			BoundStatement[] statements = new BoundStatement[before.Length + 1];
			before.CopyTo(statements, 0);
			statements[^1] = statement;
			return Block(statement.Syntax, statements);
		}
		else
		{
			BoundStatement[] statements = new BoundStatement[before.Length + after.Length + 1];
			before.CopyTo(statements, 0);
			statements[before.Length] = statement;
			after.CopyTo(statements, before.Length + 1);
			return Block(statement.Syntax, statements);
		}
	}
	#endregion

	private readonly struct ExpressionResult
	{
		/// <summary>
		/// Statements to be executed before <see cref="Expression"/> is evaluated.
		/// </summary>
		public readonly ImmutableArray<BoundStatement> Before;
		public readonly BoundExpression Expression;

		/// <summary>
		/// Statements to be executed after <see cref="Expression"/> is evaluated.
		/// </summary>
		public readonly ImmutableArray<BoundStatement> After;

		public ExpressionResult(BoundExpression expression)
		{
			Expression = expression;
		}

		public ExpressionResult(ImmutableArray<BoundStatement> before, BoundExpression expression, ImmutableArray<BoundStatement> after)
		{
			Before = before;
			Expression = expression;
			After = after;
		}

		public bool Any => !Before.IsDefaultOrEmpty || !After.IsDefaultOrEmpty;

		public int Length => Before.LengthOrZero() + 1 + After.LengthOrZero();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ExpressionResult Enclose(BoundExpression expression, ExpressionResult result)
			=> new ExpressionResult(result.Before, expression, result.After);

		public static (ImmutableArray<BoundStatement> Before, ImmutableArray<BoundStatement> After, BoundExpression Ex1, BoundExpression Ex2) Resolve(State state, ExpressionResult res1, ExpressionResult res2)
		{
			if (res1.After.IsDefaultOrEmpty && res2.Before.IsDefaultOrEmpty)
			{
				return (res1.Before, res2.After, res1.Expression, res2.Expression);
			}

			List<BoundStatement> before = [];
			ImmutableArray<BoundStatement> after;

			AddBefore(res1.Before);

			VariableSymbol res1Temp = state.GetTempVar(res1.Expression.Type);
			before.Add(Assignment(res1.Expression.Syntax, res1Temp, res1.Expression));

			AddBefore(res1.After);
			AddBefore(res2.Before);

			after = res2.After;

			return
			(
				before.ToImmutableArray(),
				after,
				Variable(res1.Expression.Syntax, res1Temp),
				res2.Expression
			);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void AddBefore(ImmutableArray<BoundStatement> arr)
			{
				if (!arr.IsDefaultOrEmpty)
				{
					before.AddRange(arr);
				}
			}
		}

		public static (ImmutableArray<BoundStatement> Before, ImmutableArray<BoundStatement> After, BoundExpression[] Expressions) Resolve(State state, ReadOnlySpan<ExpressionResult> results)
		{
			switch (results.Length)
			{
				case 0:
					return ([], [], []);
				case 1:
					return (results[0].Before, results[0].After, [results[0].Expression]);
				case 2:
					{
						var (b, a, ex1, ex2) = Resolve(state, results[0], results[1]);
						return (b, a, [ex1, ex2]);
					}
			}

			// optimize for when only the first has before (or not) and the last has after (or not)
			bool found = false;
			for (int i = 0; i < results.Length; i++)
			{
				if (i == 0)
				{
					if (!results[i].After.IsDefaultOrEmpty)
					{
						found = true;
						break;
					}
				}
				else if (i == results.Length - 1)
				{
					if (!results[i].Before.IsDefaultOrEmpty)
					{
						found = true;
						break;
					}
				}
				else if (results[i].Any)
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				BoundExpression[] expresions = new BoundExpression[results.Length];
				for (int i = 0; i < results.Length; i++)
				{
					expresions[i] = results[i].Expression;
				}

				return (results[0].Before, results[^1].After, expresions);
			}

			List<BoundStatement> before = [];
			ImmutableArray<BoundStatement> after = default;
			BoundExpression[] expressions = new BoundExpression[results.Length];

			int lastAnyIndex = -1;
			bool foundAfter = false;
			for (int i = results.Length - 1; i >= 0; i--)
			{
				ref readonly ExpressionResult res = ref results[i];

				if (!res.After.IsDefaultOrEmpty)
				{
					if (foundAfter)
					{
						lastAnyIndex = i + 1;
						break;
					}
					else
					{
						foundAfter = true;
					}
				}

				if (!res.Before.IsDefaultOrEmpty)
				{
					lastAnyIndex = i;
					break;
				}
			}

			VariableSymbol? lastTemp = null;
			for (int i = 0; i < results.Length; i++)
			{
				ref readonly ExpressionResult res = ref results[i];

				Add(res.Before);

				if (i >= lastAnyIndex)
				{
					expressions[i] = res.Expression;
					if (!res.After.IsDefaultOrEmpty)
					{
						after = res.After;
					}
				}
				else
				{
					VariableSymbol temp;
					if (lastTemp is not null && res.Before.IsDefaultOrEmpty)
					{
						temp = lastTemp;
					}
					else
					{
						temp = state.GetTempVar(res.Expression.Type);
						before.Add(Assignment(res.Expression.Syntax, temp, res.Expression));
					}

					Add(res.After);
					expressions[i] = Variable(res.Expression.Syntax, temp);

					lastTemp = res.After.IsDefaultOrEmpty ? temp : null;
				}
			}

			return
			(
				before.ToImmutableArray(),
				after,
				expressions
			);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void Add(ImmutableArray<BoundStatement> arr)
			{
				if (!arr.IsDefaultOrEmpty)
				{
					before.AddRange(arr);
				}
			}
		}

		public static (ImmutableArray<BoundStatement> Before, BoundExpression[] Expressions) ResolveBeforeOnly(State state, ReadOnlySpan<ExpressionResult> results)
		{
			switch (results.Length)
			{
				case 0:
					return ([], []);
				case 1:
					{
						var res = results[0];
						if (res.After.IsDefaultOrEmpty)
						{
							return (res.Before, [res.Expression]);
						}
						else
						{
							int beforeLen = res.Before.IsDefault ? 0 : res.Before.Length;

							var builder = ImmutableArray.CreateBuilder<BoundStatement>(beforeLen + res.After.Length + 1);

							builder.AddRange(res.Before, beforeLen);

							var tempVar = state.GetTempVar(res.Expression.Type);

							builder.Add(Assignment(res.Expression.Syntax, tempVar, res.Expression));

							builder.AddRange(res.After);

							return (builder.DrainToImmutable(), [Variable(res.Expression.Syntax, tempVar)]);
						}
					}
			}

			// optimize for when only the first has before (or not)
			bool found = false;
			for (int i = 0; i < results.Length; i++)
			{
				if (i == 0)
				{
					if (!results[i].After.IsDefaultOrEmpty)
					{
						found = true;
						break;
					}
				}
				else if (results[i].Any)
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				BoundExpression[] expresions = new BoundExpression[results.Length];
				for (int i = 0; i < results.Length; i++)
				{
					expresions[i] = results[i].Expression;
				}

				return (results[0].Before, expresions);
			}

			List<BoundStatement> before = [];
			BoundExpression[] expressions = new BoundExpression[results.Length];

			int lastAnyIndex = -1;
			for (int i = results.Length - 1; i >= 0; i--)
			{
				if (results[i].Any)
				{
					lastAnyIndex = i;
					break;
				}
			}

			VariableSymbol? lastTemp = null;
			for (int i = 0; i < results.Length; i++)
			{
				ref readonly ExpressionResult res = ref results[i];

				Add(res.Before);

				if (i > lastAnyIndex)
				{
					expressions[i] = res.Expression;
				}
				else
				{
					VariableSymbol temp;
					if (lastTemp is not null && res.Before.IsDefaultOrEmpty)
					{
						temp = lastTemp;
					}
					else
					{
						temp = state.GetTempVar(res.Expression.Type);
						before.Add(Assignment(res.Expression.Syntax, temp, res.Expression));
					}

					Add(res.After);
					expressions[i] = Variable(res.Expression.Syntax, temp);

					lastTemp = res.After.IsDefaultOrEmpty ? temp : null;
				}
			}

			return
			(
				before.ToImmutableArray(),
				expressions
			);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void Add(ImmutableArray<BoundStatement> arr)
			{
				if (!arr.IsDefaultOrEmpty)
				{
					before.AddRange(arr);
				}
			}
		}

		public bool IsSameAs(BoundExpression ex)
			=> Before.IsDefaultOrEmpty && After.IsDefaultOrEmpty && Expression == ex;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Function not yet implemented")]
		private static bool CanUseInline(BoundExpression expression)
			=> false; // TODO
	}

	private class State
	{
		private readonly FunctionSymbol _function;

		private Counter _varCounter = new Counter(0);
		private Counter _labelCounter = new Counter(0);

		public State(FunctionSymbol function)
		{
			_function = function;
		}

		public VariableSymbol GetTempVar(TypeSymbol type, bool inline = false)
		{
			VariableSymbol var = new ReservedCompilerVariableSymbol("temp", _varCounter.ToString(), inline ? Modifiers.Inline : 0, type);
			_varCounter++;
			return var;
		}

		public BoundLabel GetLabel(string name)
			=> new BoundLabel(_function.Name + "_" + name + _labelCounter++);

		public void ResetVarCount()
			=> _varCounter = new Counter(0);
	}
}
