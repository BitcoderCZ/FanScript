// <copyright file="IEmitContext.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;

namespace FanScript.Compiler.Emit;

internal interface IEmitContext
{
	DiagnosticBag Diagnostics { get; }

	BlockBuilder Builder { get; }

	IEmitStore EmitStatement(BoundStatement statement);

	IEmitStore EmitExpression(BoundExpression expression);

	IDisposable StatementBlock();

	IDisposable ExpressionBlock();

	Block AddBlock(BlockDef def);

	void Connect(IEmitStore from, IEmitStore to);

	void SetBlockValue(Block block, int valueIndex, object value);

	IEmitStore EmitLiteralExpression(object? value);

	IEmitStore EmitGetVariable(VariableSymbol variable);

	/// <summary>
	/// Emits setting <paramref name="expression"/> to <paramref name="getValueStore"/>
	/// </summary>
	/// <remarks>
	/// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
	/// </remarks>
	/// <param name="expression"></param>
	/// <param name="getValueStore"></param>
	/// <returns></returns>
	IEmitStore EmitSetExpression(BoundExpression expression, Func<IEmitStore> getValueStore);

	/// <summary>
	/// Emits setting <paramref name="variable"/> to <paramref name="getValueStore"/>
	/// </summary>
	/// <remarks>
	/// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
	/// </remarks>
	/// <param name="expression"></param>
	/// <param name="getValueStore"></param>
	/// <returns></returns>
	IEmitStore EmitSetVariable(VariableSymbol variable, Func<IEmitStore> getValueStore);

	(IEmitStore X, IEmitStore Y, IEmitStore Z) BreakVector(BoundExpression expression);

	IEmitStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent);

	object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant);

	void WriteComment(string text);

	IEmitStore EmitSetArraySegment(BoundArraySegmentExpression segment, BoundExpression arrayVariable, BoundExpression startIndex);
}
