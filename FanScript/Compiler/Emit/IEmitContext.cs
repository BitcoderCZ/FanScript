// <copyright file="IEmitContext.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols.Variables;

namespace FanScript.Compiler.Emit;

internal interface IEmitContext
{
	DiagnosticBag Diagnostics { get; }

	BlockBuilder Builder { get; }

	ITerminalStore EmitStatement(BoundStatement statement);

	ITerminalStore EmitExpression(BoundExpression expression);

	IDisposable StatementBlock();

	IDisposable ExpressionBlock();

	Block AddBlock(BlockDef def);

	void Connect(ITerminal from, ITerminal to);

	void Connect(ITerminalStore from, ITerminalStore to);

	void SetSetting(Block block, int valueIndex, object value);

	ITerminalStore EmitLiteralExpression(object? value);

	ITerminalStore EmitGetVariable(VariableSymbol variable);

	/// <summary>
	/// Emits setting <paramref name="expression"/> to <paramref name="getValueStore"/>
	/// </summary>
	/// <remarks>
	/// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
	/// </remarks>
	/// <param name="expression"></param>
	/// <param name="getValueStore"></param>
	/// <returns></returns>
	ITerminalStore EmitSetExpression(BoundExpression expression, Func<ITerminalStore> getValueStore);

	/// <summary>
	/// Emits setting <paramref name="variable"/> to <paramref name="getValueStore"/>
	/// </summary>
	/// <remarks>
	/// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
	/// </remarks>
	/// <param name="expression"></param>
	/// <param name="getValueStore"></param>
	/// <returns></returns>
	ITerminalStore EmitSetVariable(VariableSymbol variable, Func<ITerminalStore> getValueStore);

	(ITerminalStore X, ITerminalStore Y, ITerminalStore Z) BreakVector(BoundExpression expression);

	ITerminalStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent);

	object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant);

	void WriteComment(string text);

	ITerminalStore EmitSetArraySegment(BoundArraySegmentExpression segment, BoundExpression arrayVariable, BoundExpression startIndex);
}
