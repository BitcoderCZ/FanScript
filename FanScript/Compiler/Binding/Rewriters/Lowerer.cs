﻿// <copyright file="Lowerer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using System.Collections.Immutable;
using static FanScript.Compiler.Binding.BoundNodeFactory;

namespace FanScript.Compiler.Binding.Rewriters;

internal sealed class Lowerer : BoundTreeRewriter
{
	private readonly Dictionary<string, int> _labelCount = [];
	private readonly string _funcName;

	private Lowerer(string funcName)
	{
		_funcName = funcName + "_";
	}

	public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
	{
		Lowerer lowerer = new Lowerer(function.Name);
		BoundStatement lowered = lowerer.RewriteStatement(statement);
		BoundStatement result = StatementExpressionExtractor.Extract(function, lowered);
		return RemoveDeadCode(Flatten(function, result));
	}

	protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
		=> node.ConstantValue is not null ? Literal(node.Syntax, node.ConstantValue.Value) : base.RewriteBinaryExpression(node);

	protected override BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
		=> node.ConstantValue is not null ? Literal(node.Syntax, node.ConstantValue.Value) : base.RewriteUnaryExpression(node);

	protected override BoundExpression RewriteVariableExpression(BoundVariableExpression node)
		=> node.ConstantValue is not null ? Literal(node.Syntax, node.ConstantValue.Value) : base.RewriteVariableExpression(node);

	protected override BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
		=> node.Expression is BoundArraySegmentExpression expression
			? LowerArraySegmentAssignment(expression, node.Variable)
			: base.RewriteAssignmentStatement(node);

	protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
	{
		if (node.ElseStatement is null)
		{
			// if <condition>
			//      <then>
			//
			// ---->
			//
			// gotoFalse <condition> end
			// <then>
			// end:
			BoundLabel endLabel = GenerateLabel("ifEnd");
			BoundBlockStatement result = Block(
				node.Syntax,
				GotoFalse(node.Syntax, endLabel, node.Condition),
				Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockStart),
				node.ThenStatement,
				Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd),
				Label(node.Syntax, endLabel));

			return RewriteStatement(result);
		}
		else
		{
			// if <condition>
			//      <then>
			// else
			//      <else>
			//
			// ---->
			//
			// gotoFalse <condition> else
			// <then>
			// goto end
			// else:
			// <else>
			// end:
			BoundLabel elseLabel = GenerateLabel("else");
			BoundLabel endLabel = GenerateLabel("ifEnd");
			BoundBlockStatement result = Block(
				node.Syntax,
				GotoFalse(node.Syntax, elseLabel, node.Condition),
				Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockStart),
				node.ThenStatement,
				Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd),
				Goto(node.Syntax, endLabel),
				Label(node.Syntax, elseLabel),
				GetElse(),
				Label(node.Syntax, endLabel));

			return RewriteStatement(result);

			BoundStatement GetElse()
			{
				return node.ElseStatement is BoundIfStatement
					? node.ElseStatement
					: Block(
						node.Syntax,
						Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockStart),
						node.ElseStatement!,
						Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd));
			}
		}
	}

	protected override BoundStatement RewriteEventStatement(BoundEventStatement node)
	{
		BoundEventStatement newNode = (BoundEventStatement)base.RewriteEventStatement(node);

		// onPlay
		//      <body>
		//
		// ----->
		//
		// gotoTrue <special condition (play sensor, late update, ...) (handeled by emiter)> onSpecialLabel
		// goto end
		// onSpecialLabel:
		// <body>
		// goto end [rollback] // special goto that doesn't *really* "goto" but for the purposes of ControlFlowGraph does, neccesary because once body is finished the goto end will execute anyway bacause of how the special block blocks work (exec body, exec after)
		// end:
		BoundLabel onSpecialLabel = GenerateLabel("onSpecial");
		BoundLabel endLabel = GenerateLabel("end");
		BoundBlockStatement result = Block(
			newNode.Syntax,
			EventGoto(newNode.Syntax, onSpecialLabel, newNode.Type, newNode.ArgumentClause),
			Goto(newNode.Syntax, endLabel),
			Label(newNode.Syntax, onSpecialLabel),
			newNode.Block,
			Hint(newNode.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd),
			RollbackGoto(newNode.Syntax, endLabel),
			Label(newNode.Syntax, endLabel));

		return RewriteStatement(result);
	}

	protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
	{
		// while <condition>
		//      <body>
		//
		// ----->
		//
		// continue:
		// gotoFalse <condition> break
		// <body>
		// goto continue
		// break:
		BoundBlockStatement result = Block(
			node.Syntax,
			Label(node.Syntax, node.ContinueLabel),
			GotoFalse(node.Syntax, node.BreakLabel, node.Condition),
			Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockStart),
			node.Body,
			Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd),
			Goto(node.Syntax, node.ContinueLabel),
			Label(node.Syntax, node.BreakLabel));

		return RewriteStatement(result);
	}

	protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
	{
		// do
		//      <body>
		// while <condition>
		//
		// ----->
		//
		// body:
		// <body>
		// continue:
		// gotoTrue <condition> body
		// break:
		BoundLabel bodyLabel = GenerateLabel("body");
		BoundBlockStatement result = Block(
			node.Syntax,
			Label(node.Syntax, bodyLabel),
			Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockStart),
			node.Body,
			Hint(node.Syntax, BoundEmitterHintStatement.HintKind.StatementBlockEnd),
			Label(node.Syntax, node.ContinueLabel),
			GotoTrue(node.Syntax, bodyLabel, node.Condition),
			Label(node.Syntax, node.BreakLabel));

		return RewriteStatement(result);
	}

	protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
	{
		if (node.Condition.ConstantValue is not null)
		{
			bool condition = (bool)node.Condition.ConstantValue.GetValueOrDefault(TypeSymbol.Bool);
			condition = node.JumpIfTrue ? condition : !condition;
			return condition ? RewriteStatement(Goto(node.Syntax, node.Label)) : RewriteStatement(Nop(node.Syntax));
		}

		return base.RewriteConditionalGotoStatement(node);
	}

	protected override BoundStatement RewriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node)
	{
		BoundCompoundAssignmentStatement newNode = (BoundCompoundAssignmentStatement)base.RewriteCompoundAssignmentStatement(node);

		// a <op>= b
		//
		// ---->
		//
		// a = (a <op> b)
		BoundAssignmentStatement result = Assignment(
			newNode.Syntax,
			newNode.Variable,
			Binary(
				newNode.Syntax,
				Variable(newNode.Syntax, newNode.Variable),
				newNode.Op,
				newNode.Expression));

		return result;
	}

	protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
	{
		BoundCompoundAssignmentExpression newNode = (BoundCompoundAssignmentExpression)base.RewriteCompoundAssignmentExpression(node);

		// a <op>= b
		//
		// ---->
		//
		// a = (a <op> b)
		BoundAssignmentExpression result = AssignmentExpression(
			newNode.Syntax,
			newNode.Variable,
			Binary(
				newNode.Syntax,
				Variable(newNode.Syntax, newNode.Variable),
				newNode.Op,
				newNode.Expression));

		return result;
	}

	protected override BoundStatement RewriteCallStatement(BoundCallStatement node)
	{
		if (node.Function is not ConstantFunctionSymbol conFunc)
		{
			return base.RewriteCallStatement(node);
		}

		BoundCallStatement newNode = (BoundCallStatement)base.RewriteCallStatement(node);

		if (newNode.ResultVariable is null)
		{
			return newNode;
		}

		BoundConstant[] constants = new BoundConstant[conFunc.Parameters.Length];
		for (int i = 0; i < newNode.Function.Parameters.Length; i++)
		{
			if (newNode.Function.Parameters[i].Modifiers.MakesTargetReference(out _))
			{
				throw new NotImplementedException($"ref and out parameters aren't yet implemented for {nameof(ConstantFunctionSymbol)}.");
			}

			if (newNode.Arguments[i].ConstantValue is null)
			{
				return newNode;
			}

			constants[i] = newNode.Arguments[i].ConstantValue!;
		}

		return Assignment(
			newNode.Syntax,
			newNode.ResultVariable,
			Literal(newNode.Syntax, conFunc.ConstantEmit(constants)[0]));
	}

	protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
	{
		if (node.Function is not ConstantFunctionSymbol conFunc)
		{
			return base.RewriteCallExpression(node);
		}

		BoundCallExpression newNode = (BoundCallExpression)base.RewriteCallExpression(node);

		BoundConstant[] constants = new BoundConstant[conFunc.Parameters.Length];
		for (int i = 0; i < newNode.Function.Parameters.Length; i++)
		{
			if (newNode.Function.Parameters[i].Modifiers.MakesTargetReference(out _))
			{
				throw new NotImplementedException($"ref and out parameters aren't yet implemented for {nameof(ConstantFunctionSymbol)}.");
			}

			if (newNode.Arguments[i].ConstantValue is null)
			{
				return newNode;
			}

			constants[i] = newNode.Arguments[i].ConstantValue!;
		}

		return Literal(newNode.Syntax, conFunc.ConstantEmit(constants)[0]);
	}

	private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
	{
		ImmutableArray<BoundStatement>.Builder builder = ImmutableArray.CreateBuilder<BoundStatement>();
		Stack<BoundStatement> stack = new Stack<BoundStatement>();
		stack.Push(statement);

		while (stack.Count > 0)
		{
			BoundStatement current = stack.Pop();

			if (current is BoundBlockStatement block)
			{
				foreach (BoundStatement s in block.Statements.Reverse())
				{
					stack.Push(s);
				}
			}
			else
			{
				builder.Add(current);
			}
		}

		if (function.Type == TypeSymbol.Void)
		{
			if (builder.Count == 0 || CanFallThrough(builder.Last()))
			{
				builder.Add(new BoundReturnStatement(statement.Syntax, null));
			}
		}

		return new BoundBlockStatement(statement.Syntax, builder.ToImmutable());
	}

	private static bool CanFallThrough(BoundStatement boundStatement)
		=> boundStatement switch
		{
			BoundReturnStatement => false,
			BoundGotoStatement => false,
			_ => true,
		};

	private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
	{
		ControlFlowGraph controlFlow = ControlFlowGraph.Create(node);
		HashSet<BoundStatement> reachableStatements = [.. controlFlow.Blocks.SelectMany(b => b.Statements)];

		ImmutableArray<BoundStatement>.Builder builder = node.Statements.ToBuilder();
		for (int i = builder.Count - 1; i >= 0; i--)
		{
			if (!reachableStatements.Contains(builder[i]) && builder[i] is not BoundEmitterHintStatement)
			{
				builder.RemoveAt(i);
			}
		}

		return new BoundBlockStatement(node.Syntax, builder.ToImmutable());
	}

	private BoundLabel GenerateLabel(string name)
	{
		name = _funcName + name;

		int count;
		if (!_labelCount.TryGetValue(name, out count))
		{
			count = 1;
		}

		_labelCount[name] = count + 1;

		string labelName = name + count;
		return new BoundLabel(labelName);
	}

	private BoundBlockStatement LowerArraySegmentAssignment(BoundArraySegmentExpression expression, VariableSymbol arrayVariable, float startIndex = 0f)
	{
		BoundArraySegmentExpression node = (BoundArraySegmentExpression)RewriteArraySegmentExpression(expression);

		// x = [a, b, c, ...]
		//
		// ---->
		//
		// setRange(x, 0, [a, b, c, ...])
		BoundBlockStatement result = Block(
			node.Syntax,
			new BoundCallStatement(
				node.Syntax,
				BuiltinFunctions.ArraySetRange,
				new BoundArgumentClause(
					node.Syntax,
					[0, 0, 0],
					[Variable(node.Syntax, arrayVariable), Literal(node.Syntax, startIndex), expression]),
				TypeSymbol.Void,
				node.ElementType,
				null));

		return result;
	}
}
