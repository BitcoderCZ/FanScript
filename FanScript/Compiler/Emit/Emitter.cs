// <copyright file="Emitter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Scripting.Utils;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit.TerminalStores;
using FanScript.Compiler.Emit.Utils;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler.Emit;

internal sealed class Emitter : IEmitContext
{
	// key - a label before antoher label, item - the label after key
	private readonly Dictionary<string, string> _sameTargetLabels = [];

	// key - label name, item - list of goto "origins", not only gotos but also statements just before the label
	private readonly ListMultiValueDictionary<string, ITerminalStore> _gotosToConnect = [];

	// key - label name, item - the store to connect gotos to
	private readonly Dictionary<string, ITerminalStore> _afterLabel = [];

	private readonly InlineVarManager _inlineVarManager = new();

	private readonly Dictionary<VariableSymbol, BreakBlockCache> _vectorBreakCache = [];
	private readonly Dictionary<VariableSymbol, BreakBlockCache> _rotationBreakCache = [];

	private readonly Stack<List<ITerminalStore>> _beforeReturnStack = new();
	private readonly Dictionary<FunctionSymbol, ITerminalStore> _functions = [];
	private readonly ListMultiValueDictionary<FunctionSymbol, ITerminalStore> _calls = [];

	private BoundProgram _program = null!;
	private IScopedCodePlacer _placer = null!;

	private FunctionSymbol _currentFunciton = null!;

	private Emitter()
	{
	}

	public DiagnosticBag Diagnostics { get; private set; } = new DiagnosticBag();

	public BlockBuilder Builder { get; private set; } = null!;

	public static ImmutableArray<Diagnostic> Emit(BoundProgram program, ICodePlacer placer, BlockBuilder builder)
	{
		if (program.Diagnostics.HasErrors())
		{
			return program.Diagnostics;
		}
		else
		{
			Emitter emitter = new Emitter();
			return emitter.EmitInternal(program, placer is IScopedCodePlacer scoped ? scoped : new ScopedCodePlacerWrapper(placer), builder);
		}
	}

	private ImmutableArray<Diagnostic> EmitInternal(BoundProgram program, IScopedCodePlacer placer, BlockBuilder builder)
	{
		_program = program;
		_placer = placer;
		Builder = builder;

		_vectorBreakCache.Clear();
		_rotationBreakCache.Clear();

		foreach (var (func, body) in _program.Functions.ToImmutableSortedDictionary())
		{
			if (func != program.ScriptFunction && program.Analysis.ShouldFunctionGetInlined(func))
			{
				continue;
			}

			_currentFunciton = func;

			using (StatementBlock())
			{
				WriteComment(func.Name);

				_beforeReturnStack.Push([]);
				_functions.Add(func, EmitStatement(body));
				_beforeReturnStack.Pop();

				ProcessLabelsAndGotos();
			}

			_currentFunciton = null!;
		}

		ProcessCalls();

		return [.. Diagnostics, .. _program.Diagnostics];
	}

	private void ProcessLabelsAndGotos()
	{
		foreach (var (labelName, stores) in _gotosToConnect)
		{
			if (!TryGetAfterLabel(labelName, out ITerminalStore? afterLabel))
			{
				continue;
			}

			foreach (ITerminalStore store in stores)
			{
				Builder.Connect(store, afterLabel);
			}
		}

		_sameTargetLabels.Clear();
		_gotosToConnect.Clear();
		_afterLabel.Clear();

		bool TryGetAfterLabel(string name, [NotNullWhen(true)] out ITerminalStore? emitStore)
		{
			if (_afterLabel.TryGetValue(name, out emitStore))
			{
				return emitStore is not GotoTerminalStore gotoEmit || TryGetAfterLabel(gotoEmit.LabelName, out emitStore);
			}
			else if (_sameTargetLabels.TryGetValue(name, out string? target))
			{
				return TryGetAfterLabel(target, out emitStore);
			}
			else
			{
				emitStore = null;
				return false;
			}
		}
	}

	private void ProcessCalls()
	{
		foreach (var (func, callList) in _calls)
		{
			if (!_functions.TryGetValue(func, out var funcStore))
			{
				throw new Exception($"Failed to get entry point for function '{func}'.");
			}

			for (int i = 0; i < callList.Count; i++)
			{
				Connect(callList[i], funcStore);
			}
		}

		_functions.Clear();
		_calls.Clear();
	}

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitStatement(BoundStatement statement)
	{
		ITerminalStore store = statement switch
		{
			BoundBlockStatement blockStatement => EmitBlockStatement(blockStatement),
			BoundVariableDeclarationStatement variableDeclarationStatement when variableDeclarationStatement.OptionalAssignment is not null => EmitStatement(variableDeclarationStatement.OptionalAssignment),
			BoundVariableDeclarationStatement => NopTerminalStore.Instance,
			BoundAssignmentStatement assignmentStatement => EmitAssigmentStatement(assignmentStatement),
			BoundPostfixStatement postfixStatement => EmitPostfixStatement(postfixStatement),
			BoundPrefixStatement prefixStatement => EmitPrefixStatement(prefixStatement),
			BoundGotoStatement gotoStatement => EmitGotoStatement(gotoStatement),
			BoundEventGotoStatement eventGotoStatement => EmitEventGotoStatement(eventGotoStatement),
			BoundConditionalGotoStatement conditionalGotoStatement => EmitConditionalGotoStatement(conditionalGotoStatement),
			BoundLabelStatement labelStatement => EmitLabelStatement(labelStatement),
			BoundReturnStatement returnStatement => EmitReturnStatement(returnStatement),
			BoundEmitterHintStatement emitterHintStatement => EmitHint(emitterHintStatement),
			BoundCallStatement callStatement => EmitCallStatement(callStatement),
			BoundExpressionStatement expressionStatement => EmitExpressionStatement(expressionStatement),
			BoundNopStatement => NopTerminalStore.Instance,

			_ => throw new UnexpectedBoundNodeException(statement),
		};

		return store;
	}

	private ITerminalStore EmitBlockStatement(BoundBlockStatement statement)
	{
		if (statement.Statements.Length == 0)
		{
			return NopTerminalStore.Instance;
		}
		else if (statement.Statements.Length == 1 && statement.Statements[0] is BoundBlockStatement inBlock)
		{
			return EmitBlockStatement(inBlock);
		}

		TerminalConnector connector = new TerminalConnector(Connect);

		bool newCodeBlock = _placer.CurrentCodeBlockBlocks > 0;
		if (newCodeBlock)
		{
			EnterStatementBlock();
		}

		for (int i = 0; i < statement.Statements.Length; i++)
		{
			ITerminalStore statementStore = EmitStatement(statement.Statements[i]);

			connector.Add(statementStore);
		}

		if (newCodeBlock)
		{
			ExitStatementBlock();
		}

		return connector.Store;
	}

	private ITerminalStore EmitAssigmentStatement(BoundAssignmentStatement statement)
		=> EmitSetVariable(statement.Variable, statement.Expression);

	private TerminalStore EmitPostfixStatement(BoundPostfixStatement statement)
	{
		BlockDef def = statement.PostfixKind switch
		{
			PostfixKind.Increment => StockBlocks.Variables.IncrementNumber,
			PostfixKind.Decrement => StockBlocks.Variables.DecrementNumber,
			_ => throw new UnknownEnumValueException<PostfixKind>(statement.PostfixKind),
		};
		Block block = AddBlock(def);

		using (ExpressionBlock())
		{
			ITerminalStore store = EmitGetVariable(statement.Variable);

			Connect(store, TerminalStore.CIn(block, block.Type["Variable"]));
		}

		return new TerminalStore(block);
	}

	private TerminalStore EmitPrefixStatement(BoundPrefixStatement statement)
	{
		BlockDef def = statement.PrefixKind switch
		{
			PrefixKind.Increment => StockBlocks.Variables.IncrementNumber,
			PrefixKind.Decrement => StockBlocks.Variables.DecrementNumber,
			_ => throw new UnknownEnumValueException<PrefixKind>(statement.PrefixKind),
		};
		Block block = AddBlock(def);

		using (ExpressionBlock())
		{
			ITerminalStore store = EmitGetVariable(statement.Variable);

			Connect(store, TerminalStore.CIn(block, block.Type["Variable"]));
		}

		return new TerminalStore(block);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "whomp whomp")]
	private static ITerminalStore EmitGotoStatement(BoundGotoStatement statement)
		=> statement.IsRollback ? RollbackTerminalStore.Instance : new GotoTerminalStore(statement.Label.Name);

	private TerminalStore EmitEventGotoStatement(BoundEventGotoStatement statement)
	{
		BlockDef def = statement.EventType switch
		{
			EventType.Play => StockBlocks.Control.PlaySensor,
			EventType.LateUpdate => StockBlocks.Control.LateUpdate,
			EventType.BoxArt => StockBlocks.Control.BoxArtSensor,
			EventType.Touch => StockBlocks.Control.TouchSensor,
			EventType.Swipe => StockBlocks.Control.SwipeSensor,
			EventType.Button => StockBlocks.Control.Button,
			EventType.Collision => StockBlocks.Control.Collision,
			EventType.Loop => StockBlocks.Control.Loop,
			_ => throw new UnknownEnumValueException<EventType>(statement.EventType),
		};
		Block block = AddBlock(def);

		ImmutableArray<BoundExpression>? arguments = statement.ArgumentClause?.Arguments;

		EnterStatementBlock();

		switch (statement.EventType)
		{
			case EventType.Touch:
				{
					object?[]? values = ValidateConstants(arguments!.Value.AsMemory(2..), true);
					if (values is null)
					{
						break;
					}

					for (int i = 0; i < values.Length; i++)
					{
						SetSetting(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
					}

					ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(..2)));
					return new TerminalStore(block);
				}

			case EventType.Swipe:
				{
					ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan()));
					return new TerminalStore(block);
				}

			case EventType.Button:
				{
					object?[]? values = ValidateConstants(arguments!.Value.AsMemory(), true);
					if (values is null)
					{
						break;
					}

					for (int i = 0; i < values.Length; i++)
					{
						SetSetting(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
					}
				}

				break;
			case EventType.Collision:
				{
					ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(1..), arguments!.Value.AsMemory(..1)));
					return new TerminalStore(block);
				}

			case EventType.Loop:
				{
					ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(2..), arguments!.Value.AsMemory(..2)));
					return new TerminalStore(block);
				}
		}

		ConnectToLabel(statement.Label.Name, TerminalStore.COut(block, block.Type.Terminals.Get(^2)));

		return new TerminalStore(block);

		ITerminalStore PlaceAndConnectRefArgs(ReadOnlySpan<BoundExpression> outArguments, ReadOnlyMemory<BoundExpression>? arguments = null)
		{
			arguments ??= ReadOnlyMemory<BoundExpression>.Empty;

			TerminalConnector connector = new TerminalConnector(Connect);
			connector.Add(TerminalStore.COut(block, block.Type.Terminals.Get(Index.FromEnd(2 + arguments.Value.Length))));

			if (arguments.Value.Length != 0)
			{
				ExitStatementBlock();

				using (ExpressionBlock())
				{
					var argumentsSpan = arguments.Value.Span;

					for (int i = 0; i < argumentsSpan.Length; i++)
					{
						ITerminalStore store = EmitExpression(argumentsSpan[i]);

						Connect(store, TerminalStore.CIn(block, block.Type.Terminals.Get(Index.FromEnd(i + 2))));
					}
				}

				EnterStatementBlock();
			}

			for (int i = 0; i < outArguments.Length; i++)
			{
				VariableSymbol variable = ((BoundVariableExpression)outArguments[i]).Variable;

				connector.Add(EmitSetVariable(variable, () => TerminalStore.COut(block, block.Type.Terminals.Get(Index.FromEnd(i + arguments.Value.Length + 3)))));
			}

			return connector.Store;
		}
	}

	private ConditionalGotoTerminalStore EmitConditionalGotoStatement(BoundConditionalGotoStatement statement)
	{
		Block block = AddBlock(StockBlocks.Control.If);

		using (ExpressionBlock())
		{
			ITerminalStore condition = EmitExpression(statement.Condition);

			Connect(condition, TerminalStore.CIn(block, block.Type["Condition"]));
		}

		ConditionalGotoTerminalStore store = new ConditionalGotoTerminalStore(
			block,
			block.Type.Before,
			block,
			block.Type[statement.JumpIfTrue ? "True" : "False"],
			block,
			block.Type[statement.JumpIfTrue ? "False" : "True"]);

		ConnectToLabel(statement.Label.Name, TerminalStore.COut(store.OnCondition, store.OnConditionTerminal));

		return store;
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "whomp whomp")]
	private static LabelTerminalStore EmitLabelStatement(BoundLabelStatement statement)
		=> new LabelTerminalStore(statement.Label.Name);

	private ITerminalStore EmitReturnStatement(BoundReturnStatement statement)
		=> statement.Expression is null
			? new ReturnTerminalStore()
			: EmitSetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(_currentFunciton), statement.Expression);

	private NopTerminalStore EmitHint(BoundEmitterHintStatement statement)
	{
		switch (statement.Hint)
		{
			case BoundEmitterHintStatement.HintKind.StatementBlockStart:
				EnterStatementBlock();
				break;
			case BoundEmitterHintStatement.HintKind.StatementBlockEnd:
				ExitStatementBlock();
				break;
			case BoundEmitterHintStatement.HintKind.HighlightStart:
				_placer.EnterHighlight();
				break;
			case BoundEmitterHintStatement.HintKind.HighlightEnd:
				_placer.ExitHightlight();
				break;
			default:
				throw new UnknownEnumValueException<BoundEmitterHintStatement.HintKind>(statement.Hint);
		}

		return NopTerminalStore.Instance;
	}

	private ITerminalStore EmitCallStatement(BoundCallStatement statement)
	{
		if (statement.Function is BuiltinFunctionSymbol builtinFunction)
		{
			return builtinFunction.Emit(new BoundCallExpression(statement.Syntax, statement.Function, statement.ArgumentClause, statement.ReturnType, statement.GenericType), this);
		}

		Debug.Assert(!statement.Function.Modifiers.HasFlag(Modifiers.Inline), "Calls to inline funcitons should get inlined.");

		FunctionSymbol func = statement.Function;

		TerminalConnector connector = new TerminalConnector(Connect);

		for (int i = 0; i < func.Parameters.Length; i++)
		{
			Modifiers mods = func.Parameters[i].Modifiers;

			if (mods.HasFlag(Modifiers.Out))
			{
				continue;
			}

			ITerminalStore setStore = EmitSetVariable(func.Parameters[i], statement.ArgumentClause.Arguments[i]);

			connector.Add(setStore);
		}

		Block callBlock = AddBlock(StockBlocks.Control.If);

		using (ExpressionBlock())
		{
			Block trueBlock = AddBlock(StockBlocks.Values.True);
			Connect(TerminalStore.COut(trueBlock, trueBlock.Type["True"]), TerminalStore.CIn(callBlock, callBlock.Type["Condition"]));
		}

		_calls.Add(func, TerminalStore.COut(callBlock, callBlock.Type["True"]));

		connector.Add(new TerminalStore(callBlock));

		for (int i = 0; i < func.Parameters.Length; i++)
		{
			Modifiers mods = func.Parameters[i].Modifiers;

			if (!mods.HasFlag(Modifiers.Out) && !mods.HasFlag(Modifiers.Ref))
			{
				continue;
			}

			ITerminalStore setStore = EmitSetExpression(statement.ArgumentClause.Arguments[i], () =>
			{
				using (ExpressionBlock())
				{
					return EmitGetVariable(func.Parameters[i]);
				}
			});

			connector.Add(setStore);
		}

		if (func.Type != TypeSymbol.Void && statement.ResultVariable is not null)
		{
			ITerminalStore setStore = EmitSetVariable(statement.ResultVariable, () =>
			{
				using (ExpressionBlock())
				{
					return EmitGetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(func));
				}
			});

			connector.Add(setStore);
		}

		return connector.Store;
	}

	private ITerminalStore EmitExpressionStatement(BoundExpressionStatement statement)
		=> statement.Expression is BoundNopExpression ? NopTerminalStore.Instance : EmitExpression(statement.Expression);

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitExpression(BoundExpression expression)
	{
		ITerminalStore store = expression switch
		{
			BoundLiteralExpression literalExpression => EmitLiteralExpression(literalExpression),
			BoundConstructorExpression constructorExpression => EmitConstructorExpression(constructorExpression),
			BoundUnaryExpression unaryExpression => EmitUnaryExpression(unaryExpression),
			BoundBinaryExpression binaryExpression => EmitBinaryExpression(binaryExpression),
			BoundVariableExpression variableExpression => EmitVariableExpression(variableExpression),
			BoundCallExpression callExpression => EmitCallExpression(callExpression),

			_ => throw new UnexpectedBoundNodeException(expression),
		};

		return store;
	}

	private ITerminalStore EmitLiteralExpression(BoundLiteralExpression expression)
		=> EmitLiteralExpression(expression.Value);

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitLiteralExpression(object? value)
	{
		if (value is null)
		{
			return NopTerminalStore.Instance;
		}

		Block block = AddBlock(StockBlocks.Values.ValueByType(value));

		if (value is not bool)
		{
			SetSetting(block, 0, value);
		}

		return TerminalStore.COut(block, block.Type.Terminals[0]);
	}

	private ITerminalStore EmitConstructorExpression(BoundConstructorExpression expression)
	{
		if (expression.ConstantValue is not null)
		{
			return EmitLiteralExpression(expression.ConstantValue.Value);
		}

		BlockDef def = StockBlocks.Math.MakeByType(expression.Type.ToWireType());
		Block block = AddBlock(def);

		using (ExpressionBlock())
		{
			ITerminalStore xStore = EmitExpression(expression.ExpressionX);
			ITerminalStore yStore = EmitExpression(expression.ExpressionY);
			ITerminalStore zStore = EmitExpression(expression.ExpressionZ);

			Connect(xStore, TerminalStore.CIn(block, def["X"]));
			Connect(yStore, TerminalStore.CIn(block, def["Y"]));
			Connect(zStore, TerminalStore.CIn(block, def["Z"]));
		}

		return TerminalStore.COut(block, def.Terminals[0]);
	}

	private ITerminalStore EmitUnaryExpression(BoundUnaryExpression expression)
	{
		switch (expression.Op.Kind)
		{
			case BoundUnaryOperatorKind.Identity:
				return EmitExpression(expression.Operand);
			case BoundUnaryOperatorKind.Negation:
				{
					if (expression.Type == TypeSymbol.Vector3)
					{
						Block block = AddBlock(StockBlocks.Math.Multiply_Vector);

						using (ExpressionBlock())
						{
							ITerminalStore opStore = EmitExpression(expression.Operand);

							Block numb = AddBlock(StockBlocks.Values.Number);
							SetSetting(numb, 0, -1f);

							Connect(opStore, TerminalStore.CIn(block, block.Type["Vec"]));
							Connect(TerminalStore.COut(numb, numb.Type["Number"]), TerminalStore.CIn(block, block.Type["Num"]));
						}

						return TerminalStore.COut(block, block.Type["Vec * Num"]);
					}
					else
					{
						Block block = AddBlock(expression.Type == TypeSymbol.Float ? StockBlocks.Math.Negate : StockBlocks.Math.Inverse);

						using (ExpressionBlock())
						{
							ITerminalStore opStore = EmitExpression(expression.Operand);

							Connect(opStore, TerminalStore.CIn(block, block.Type.Terminals[1]));
						}

						return TerminalStore.COut(block, block.Type.Terminals[0]);
					}
				}

			case BoundUnaryOperatorKind.LogicalNegation:
				{
					Block block = AddBlock(StockBlocks.Math.Not);

					using (ExpressionBlock())
					{
						ITerminalStore opStore = EmitExpression(expression.Operand);

						Connect(opStore, TerminalStore.CIn(block, block.Type["Tru"]));
					}

					return TerminalStore.COut(block, block.Type["Not Tru"]);
				}

			default:
				throw new UnknownEnumValueException<BoundUnaryOperatorKind>(expression.Op.Kind);
		}
	}

	private ITerminalStore EmitBinaryExpression(BoundBinaryExpression expression)
		=> expression.Type == TypeSymbol.Bool || expression.Type == TypeSymbol.Float
			? EmitBinaryExpression_FloatOrBool(expression)
			: expression.Type == TypeSymbol.Vector3 || expression.Type == TypeSymbol.Rotation
			? EmitBinaryExpression_VecOrRot(expression)
			: throw new UnexpectedSymbolException(expression.Type);

	private TerminalStore EmitBinaryExpression_FloatOrBool(BoundBinaryExpression expression)
	{
		BlockDef op = expression.Op.Kind switch
		{
			BoundBinaryOperatorKind.Addition => StockBlocks.Math.Add_Number,
			BoundBinaryOperatorKind.Subtraction => StockBlocks.Math.Subtract_Number,
			BoundBinaryOperatorKind.Multiplication => StockBlocks.Math.Multiply_Number,
			BoundBinaryOperatorKind.Division => StockBlocks.Math.Divide_Number,
			BoundBinaryOperatorKind.Modulo => StockBlocks.Math.Modulo_Number,
			BoundBinaryOperatorKind.Equals or BoundBinaryOperatorKind.NotEquals => StockBlocks.Math.EqualsByType(expression.Left.Type!.ToWireType()),
			BoundBinaryOperatorKind.LogicalAnd => StockBlocks.Math.LogicalAnd,
			BoundBinaryOperatorKind.LogicalOr => StockBlocks.Math.LogicalOr,
			BoundBinaryOperatorKind.Less or BoundBinaryOperatorKind.GreaterOrEquals => StockBlocks.Math.Less,
			BoundBinaryOperatorKind.Greater or BoundBinaryOperatorKind.LessOrEquals => StockBlocks.Math.Greater,
			_ => throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind),
		};

		if (expression.Op.Kind == BoundBinaryOperatorKind.NotEquals
			|| expression.Op.Kind == BoundBinaryOperatorKind.LessOrEquals
			|| expression.Op.Kind == BoundBinaryOperatorKind.GreaterOrEquals)
		{
			// invert output, >= or <=, >= can be accomplished as inverted <
			Block not = AddBlock(StockBlocks.Math.Not);
			using (ExpressionBlock())
			{
				Block block = AddBlock(op);

				Connect(TerminalStore.COut(block, block.Type.Terminals[0]), TerminalStore.CIn(not, not.Type["Tru"]));

				using (ExpressionBlock())
				{
					ITerminalStore store0 = EmitExpression(expression.Left);
					ITerminalStore store1 = EmitExpression(expression.Right);

					Connect(store0, TerminalStore.CIn(block, block.Type.Terminals[2]));
					Connect(store1, TerminalStore.CIn(block, block.Type.Terminals[1]));
				}
			}

			return TerminalStore.COut(not, not.Type["Not Tru"]);
		}
		else
		{
			Block block = AddBlock(op);
			using (ExpressionBlock())
			{
				ITerminalStore store0 = EmitExpression(expression.Left);
				ITerminalStore store1 = EmitExpression(expression.Right);

				Connect(store0, TerminalStore.CIn(block, block.Type.Terminals[2]));
				Connect(store1, TerminalStore.CIn(block, block.Type.Terminals[1]));
			}

			return TerminalStore.COut(block, block.Type.Terminals[0]);
		}
	}

	private ITerminalStore EmitBinaryExpression_VecOrRot(BoundBinaryExpression expression)
	{
		BlockDef? defOp = null;
		switch (expression.Op.Kind)
		{
			case BoundBinaryOperatorKind.Addition:
				if (expression.Left.Type == TypeSymbol.Vector3)
				{
					defOp = StockBlocks.Math.Add_Vector;
				}

				break;
			case BoundBinaryOperatorKind.Subtraction:
				if (expression.Left.Type == TypeSymbol.Vector3)
				{
					defOp = StockBlocks.Math.Subtract_Vector;
				}

				break;
			case BoundBinaryOperatorKind.Multiplication:
				defOp = expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Float
					? StockBlocks.Math.Multiply_Vector
					: expression.Left.Type == TypeSymbol.Rotation && expression.Right.Type == TypeSymbol.Rotation
					? StockBlocks.Math.Multiply_Rotation
					: expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Rotation
					? StockBlocks.Math.Rotate_Vector
					: throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
				break;
			case BoundBinaryOperatorKind.Division:
			case BoundBinaryOperatorKind.Modulo:
				break; // supported, but not one block
			case BoundBinaryOperatorKind.Equals:
				defOp = expression.Left.Type == TypeSymbol.Vector3
					? StockBlocks.Math.Equals_Vector
					: throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);

				// rotation doesn't have equals???
				break;
			case BoundBinaryOperatorKind.NotEquals:
				break; // supported, but not one block
			default:
				throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
		}

		if (defOp is null)
		{
			switch (expression.Op.Kind)
			{
				case BoundBinaryOperatorKind.Addition: // Rotation
					return BuildOperatorWithBreak(StockBlocks.Math.Make_Rotation, StockBlocks.Math.Add_Number);
				case BoundBinaryOperatorKind.Subtraction: // Rotation
					return BuildOperatorWithBreak(StockBlocks.Math.Make_Rotation, StockBlocks.Math.Subtract_Number);
				case BoundBinaryOperatorKind.Division: // Vector3
					return BuildOperatorWithBreak(StockBlocks.Math.Make_Vector, StockBlocks.Math.Divide_Number);
				case BoundBinaryOperatorKind.Modulo: // Vector3
					return BuildOperatorWithBreak(StockBlocks.Math.Make_Vector, StockBlocks.Math.Modulo_Number);
				case BoundBinaryOperatorKind.NotEquals:
					{
						defOp = expression.Left.Type == TypeSymbol.Vector3
							? StockBlocks.Math.Equals_Vector
							: throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);

						Block not = AddBlock(StockBlocks.Math.Not);
						using (ExpressionBlock())
						{
							Block op = AddBlock(defOp);

							Connect(TerminalStore.COut(op, op.Type.Terminals[0]), TerminalStore.CIn(not, not.Type["Tru"]));

							using (ExpressionBlock())
							{
								ITerminalStore store0 = EmitExpression(expression.Left);
								ITerminalStore store1 = EmitExpression(expression.Right);

								Connect(store0, TerminalStore.CIn(op, op.Type.Terminals[2]));
								Connect(store1, TerminalStore.CIn(op, op.Type.Terminals[1]));
							}
						}

						return TerminalStore.COut(not, not.Type["Not Tru"]);
					}

				default:
					throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
			}
		}
		else
		{
			Block op = AddBlock(defOp);
			using (ExpressionBlock())
			{
				ITerminalStore store0 = EmitExpression(expression.Left);
				ITerminalStore store1 = EmitExpression(expression.Right);

				Connect(store0, TerminalStore.CIn(op, op.Type.Terminals[2]));
				Connect(store1, TerminalStore.CIn(op, op.Type.Terminals[1]));
			}

			return TerminalStore.COut(op, op.Type.Terminals[0]);
		}

		ITerminalStore BuildOperatorWithBreak(BlockDef defMake, BlockDef defOp)
		{
			Block make = AddBlock(defMake);

			using (ExpressionBlock())
			{
				Block op1 = AddBlock(defOp);

				ITerminalStore leftX;
				ITerminalStore leftY;
				ITerminalStore leftZ;
				ITerminalStore rightX;
				ITerminalStore rightY;
				ITerminalStore rightZ;
				using (ExpressionBlock())
				{
					(leftX, leftY, leftZ) = BreakVector(expression.Left);

					if (expression.Right.Type == TypeSymbol.Vector3 || expression.Right.Type == TypeSymbol.Rotation)
					{
						(rightX, rightY, rightZ) = BreakVector(expression.Right);
					}
					else
					{
						rightZ = rightY = rightX = EmitExpression(expression.Right);
					}
				}

				Block op2 = AddBlock(defOp);
				Block op3 = AddBlock(defOp);

				// left to op
				Connect(leftX, TerminalStore.CIn(op1, op1.Type.Terminals[2]));
				Connect(leftY, TerminalStore.CIn(op2, op2.Type.Terminals[2]));
				Connect(leftZ, TerminalStore.CIn(op3, op3.Type.Terminals[2]));

				// right to op
				Connect(rightX, TerminalStore.CIn(op1, op1.Type.Terminals[1]));
				Connect(rightY, TerminalStore.CIn(op2, op2.Type.Terminals[1]));
				Connect(rightZ, TerminalStore.CIn(op3, op3.Type.Terminals[1]));

				// op to make
				Connect(TerminalStore.COut(op1, op1.Type.Terminals[0]), TerminalStore.CIn(make, make.Type.Terminals[3]));
				Connect(TerminalStore.COut(op2, op2.Type.Terminals[0]), TerminalStore.CIn(make, make.Type.Terminals[2]));
				Connect(TerminalStore.COut(op3, op3.Type.Terminals[0]), TerminalStore.CIn(make, make.Type.Terminals[1]));
			}

			return TerminalStore.COut(make, make.Type.Terminals[0]);
		}
	}

	private ITerminalStore EmitVariableExpression(BoundVariableExpression expression)
		=> EmitGetVariable(expression.Variable);

	private ITerminalStore EmitCallExpression(BoundCallExpression expression)
	{
		if (expression.Function is BuiltinFunctionSymbol builtinFunction)
		{
			return builtinFunction.Emit(expression, this);
		}

		Debug.Fail("User defined function calls should be extracted to statement calls.");
		return NopTerminalStore.Instance;
	}

	#region Utils
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitGetVariable(VariableSymbol variable)
	{
		switch (variable)
		{
			case PropertySymbol property:
				return property.Definition.EmitGet.Invoke(this, property.Expression);
			case NullVariableSymbol:
				return NopTerminalStore.Instance;
			default:
				{
					if (variable.Modifiers.HasFlag(Modifiers.Inline))
					{
						if (_inlineVarManager.TryGet(variable, this, out var store))
						{
							return store;
						}
						else
						{
							Diagnostics.ReportTooManyInlineVariableUses(Text.TextLocation.None, variable.Name);
							return NopTerminalStore.Instance;
						}
					}

					Block block = AddBlock(StockBlocks.Variables.GetVariableByType(variable.Type!.ToWireType()));

					SetSetting(block, 0, variable.ResultName);

					return TerminalStore.COut(block, block.Type.Terminals[0]);
				}
		}
	}

	private ITerminalStore EmitSetExpression(BoundExpression expression, BoundExpression valueExpression)
		=> EmitSetExpression(expression, () =>
		{
			using (ExpressionBlock())
			{
				ITerminalStore store = EmitExpression(valueExpression);

				return store;
			}
		});

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitSetExpression(BoundExpression expression, Func<ITerminalStore> getValueStore)
	{
		switch (expression)
		{
			case BoundVariableExpression var:
				return EmitSetVariable(var.Variable, getValueStore);
			default:
				{
					Block set = AddBlock(StockBlocks.Variables.SetPtrByType(expression.Type.ToWireType()));

					ITerminalStore exStore = EmitExpression(expression);
					ITerminalStore valStore = getValueStore();

					Connect(exStore, TerminalStore.CIn(set, set.Type["Variable"]));
					Connect(valStore, TerminalStore.CIn(set, set.Type["Value"]));

					return new TerminalStore(set);
				}
		}
	}

	private ITerminalStore EmitSetVariable(VariableSymbol variable, BoundExpression expression)
		=> EmitSetVariable(variable, () =>
		{
			using (ExpressionBlock())
			{
				ITerminalStore store = EmitExpression(expression);
				return store;
			}
		});

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	public ITerminalStore EmitSetVariable(VariableSymbol variable, Func<ITerminalStore> getValueStore)
	{
		switch (variable)
		{
			case PropertySymbol property:
				return property.Definition.EmitSet!.Invoke(this, property.Expression, getValueStore);
			case NullVariableSymbol:
				return NopTerminalStore.Instance;
			default:
				{
					if (variable.Modifiers.HasFlag(Modifiers.Constant))
					{
						return NopTerminalStore.Instance;
					}
					else if (variable.Modifiers.HasFlag(Modifiers.Inline))
					{
						_inlineVarManager.Set(variable, getValueStore());
						return NopTerminalStore.Instance;
					}

					Block block = AddBlock(StockBlocks.Variables.SetVariableByType(variable.Type.ToWireType()));

					SetSetting(block, 0, variable.ResultName);

					ITerminalStore valueStore = getValueStore();

					Connect(valueStore, TerminalStore.CIn(block, block.Type.Terminals[1]));

					return new TerminalStore(block);
				}
		}
	}

	/// <summary>
	/// Breaks a vector expression into (x, y, z)
	/// </summary>
	/// <remarks>This method is optimised and may not use the <see cref="StockBlocks.Math.Break_Vector"/>/Rotation blocks</remarks>
	/// <param name="expression">The vector expression; <see cref="BoundLiteralExpression"/>, <see cref="BoundConstructorExpression"/> or <see cref="BoundVariableExpression"/></param>
	/// <returns>(x, y, z)</returns>
	/// <exception cref="InvalidDataException"></exception>
	public (ITerminalStore X, ITerminalStore Y, ITerminalStore Z) BreakVector(BoundExpression expression)
	{
		var result = BreakVectorAny(expression, [true, true, true]);
		return (result[0]!, result[1]!, result[2]!);
	}

	public ITerminalStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent)
	{
		ArgumentNullException.ThrowIfNull(expression);
		ArgumentNullException.ThrowIfNull(useComponent);
		if (useComponent.Length != 3)
		{
			throw new ArgumentException(nameof(useComponent), $"{nameof(useComponent)}.Length must be 3, not '{useComponent.Length}'");
		}

		float3? vector = null;
		if (expression is BoundLiteralExpression literal)
		{
			vector = literal.Value is float3 vec
				? vec
				: literal.Value is Rotation rot
				? (float3?)rot.Value
				: throw new InvalidDataException($"Invalid value type '{literal.Value?.GetType()}'");
		}
		else if (expression is BoundConstructorExpression contructor && contructor.ConstantValue is not null)
		{
			vector = contructor.ConstantValue.Value is float3 ?
				(float3)contructor.ConstantValue.Value :
				((Rotation)contructor.ConstantValue.Value!).Value;
		}
		else if (expression is BoundVariableExpression variable && variable.Variable.Modifiers.HasFlag(Modifiers.Constant) && variable.ConstantValue is not null)
		{
			vector = variable.ConstantValue.Value is float3 ?
				(float3)variable.ConstantValue.Value :
				((Rotation)variable.ConstantValue.Value!).Value;
		}

		if (vector is not null)
		{
			return [
				useComponent[0] ? EmitLiteralExpression(vector.Value.X) : null,
				useComponent[1] ? EmitLiteralExpression(vector.Value.Y) : null,
				useComponent[2] ? EmitLiteralExpression(vector.Value.Z) : null,
			];
		}
		else if (expression is BoundConstructorExpression contructor)
		{
			return [
				useComponent[0] ? EmitExpression(contructor.ExpressionX) : null,
				useComponent[1] ? EmitExpression(contructor.ExpressionY) : null,
				useComponent[2] ? EmitExpression(contructor.ExpressionZ) : null,
			];
		}
		else if (expression is BoundVariableExpression var)
		{
			BreakBlockCache cache = (var.Type == TypeSymbol.Vector3 ? _vectorBreakCache : _rotationBreakCache)
				.AddIfAbsent(var.Variable, new BreakBlockCache(null, 3));
			if (!cache.TryGet(out Block? block))
			{
				block = AddBlock(var.Type == TypeSymbol.Vector3 ? StockBlocks.Math.Break_Vector : StockBlocks.Math.Break_Rotation);
				cache.SetNewBlock(block);

				using (ExpressionBlock())
				{
					ITerminalStore store = EmitVariableExpression(var);
					Connect(store, TerminalStore.CIn(block, block.Type.Terminals[3]));
				}
			}

			return [
				useComponent[0] ? TerminalStore.COut(block, block.Type.Terminals[2]) : null,
				useComponent[1] ? TerminalStore.COut(block, block.Type.Terminals[1]) : null,
				useComponent[2] ? TerminalStore.COut(block, block.Type.Terminals[0]) : null,
			];
		}
		else
		{
			// just break it
			Block block = AddBlock(expression.Type == TypeSymbol.Vector3 ? StockBlocks.Math.Break_Vector : StockBlocks.Math.Break_Rotation);

			using (ExpressionBlock())
			{
				ITerminalStore store = EmitExpression(expression);
				Connect(store, TerminalStore.CIn(block, block.Type.Terminals[3]));
			}

			return [
				useComponent[0] ? TerminalStore.COut(block, block.Type.Terminals[2]) : null,
				useComponent[1] ? TerminalStore.COut(block, block.Type.Terminals[1]) : null,
				useComponent[2] ? TerminalStore.COut(block, block.Type.Terminals[0]) : null,
			];
		}
	}

	public object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant)
	{
		ReadOnlySpan<BoundExpression> expressionsMem = expressions.Span;

		object?[] values = new object?[expressionsMem.Length];
		bool invalid = false;

		for (int i = 0; i < expressionsMem.Length; i++)
		{
			BoundConstant? constant = expressionsMem[i].ConstantValue;
			if (constant is null)
			{
				if (mustBeConstant)
				{
					Diagnostics.ReportValueMustBeConstant(expressionsMem[i].Syntax.Location);
				}

				invalid = true;
			}
			else
			{
				values[i] = constant.Value;
			}
		}

		return invalid ? null : values;
	}

	public void WriteComment(string text)
	{
		foreach (string line in StringExtensions.SplitByMaxLength(text, FancadeConstants.MaxCommentLength))
		{
			Block block = AddBlock(StockBlocks.Values.Comment);
			SetSetting(block, 0, line);
		}
	}

	public ITerminalStore EmitSetArraySegment(BoundArraySegmentExpression segment, BoundExpression arrayVariable, BoundExpression startIndex)
	{
		Debug.Assert(arrayVariable.Type.InnerType == segment.ElementType, "Inner type of array variable");
		Debug.Assert(startIndex.Type == TypeSymbol.Float, $"The type of {nameof(startIndex)} must be float.");

		WireType type = arrayVariable.Type.ToWireType();

		TerminalConnector connector = new TerminalConnector(Connect);

		ITerminalStore? lastElementStore = null;

		for (int i = 0; i < segment.Elements.Length; i++)
		{
			if (i == 0 && startIndex.ConstantValue is not null && (float)startIndex.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
			{
				connector.Add(EmitSetExpression(arrayVariable, segment.Elements[i]));
			}
			else
			{
				Block setBlock = AddBlock(StockBlocks.Variables.SetPtrByType(type));

				connector.Add(new TerminalStore(setBlock));

				using (ExpressionBlock())
				{
					Block listBlock = AddBlock(StockBlocks.Variables.ListByType(type));

					Connect(TerminalStore.COut(listBlock, listBlock.Type["Element"]), TerminalStore.CIn(setBlock, setBlock.Type["Variable"]));

					using (ExpressionBlock())
					{
						lastElementStore ??= EmitExpression(arrayVariable);

						Connect(lastElementStore, TerminalStore.CIn(listBlock, listBlock.Type["Variable"]));

						lastElementStore = TerminalStore.COut(listBlock, listBlock.Type["Element"]);

						Connect(i == 0 ? EmitExpression(startIndex) : EmitLiteralExpression(1f), TerminalStore.CIn(listBlock, listBlock.Type["Index"]));
					}

					Connect(EmitExpression(segment.Elements[i]), TerminalStore.CIn(setBlock, setBlock.Type["Value"]));
				}
			}
		}

		return connector.Store;
	}
	#endregion

	#region CodePlacer and BlockBuilder redirects
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Block AddBlock(BlockDef def)
		=> _placer.PlaceBlock(def);

	public void Connect(ITerminalStore from, ITerminalStore to)
	{
		while (from is MultiTerminalStore multi)
		{
			from = multi.OutStore;
		}

		while (to is MultiTerminalStore multi)
		{
			to = multi.InStore;
		}

		if (from is RollbackTerminalStore || to is RollbackTerminalStore)
		{
			if (to is ReturnTerminalStore && from is not RollbackTerminalStore && _beforeReturnStack.Count > 0)
			{
				_beforeReturnStack.Peek().Add(from);
			}

			return;
		}

		if (from is LabelTerminalStore sameFrom && to is LabelTerminalStore sameTo)
		{
			_sameTargetLabels.Add(sameFrom.Name, sameTo.Name);
		}
		else if (from is GotoTerminalStore)
		{
			// ignore, the going to is handeled if to is GotoTerminalStore
		}
		else if (from is LabelTerminalStore fromLabel)
		{
			_afterLabel.Add(fromLabel.Name, to);
		}
		else if (to is LabelTerminalStore toLabel)
		{
			ConnectToLabel(toLabel.Name, from); // normal block before label, connect to block after the label
		}
		else if (to is GotoTerminalStore toGoto)
		{
			ConnectToLabel(toGoto.LabelName, from);
		}
		else
		{
			Builder.Connect(from, to);
		}
	}

	public void Connect(ITerminal from, ITerminal to)
		=> _placer.Connect(from, to);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ConnectToLabel(string labelName, ITerminalStore store)
		=> _gotosToConnect.Add(labelName, store);

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetSetting(Block block, int valueIndex, object value)
		=> Builder.SetSetting(block, valueIndex, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnterStatementBlock()
		=> _placer.EnterStatementBlock();

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IDisposable StatementBlock()
		=> _placer.StatementBlock();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ExitStatementBlock()
		=> _placer.ExitStatementBlock();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnterExpressionBlock()
		=> _placer.EnterExpressionBlock();

	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IDisposable ExpressionBlock()
		=> _placer.ExpressionBlock();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ExitExpressionBlock()
		=> _placer.ExitExpressionBlock();
	#endregion
}
