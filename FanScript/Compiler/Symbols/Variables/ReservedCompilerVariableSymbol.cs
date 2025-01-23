// <copyright file="ReservedCompilerVariableSymbol.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting;
using FanScript.Compiler.Symbols.Functions;
using System.Diagnostics;

namespace FanScript.Compiler.Symbols.Variables;

public sealed class ReservedCompilerVariableSymbol : CompilerVariableSymbol
{
	public ReservedCompilerVariableSymbol(string identifier, string name, Modifiers modifiers, TypeSymbol type)
		: base(name, modifiers, type)
	{
		Debug.Assert(!string.IsNullOrEmpty(identifier) && identifier.Length + 2 <= FancadeConstants.MaxVariableNameLength, $"{nameof(identifier)} cannot be empty or longer than {FancadeConstants.MaxVariableNameLength + 2}.");

		Identifier = identifier;
	}

	public string Identifier { get; }

	public static ReservedCompilerVariableSymbol CreateParam(FunctionSymbol func, int paramIndex)
		=> new ReservedCompilerVariableSymbol("func" + func.Id.ToString(), paramIndex.ToString(), func.Parameters[paramIndex].Modifiers, func.Parameters[paramIndex].Type);

	public static ReservedCompilerVariableSymbol CreateFunctionRes(FunctionSymbol func, bool inlineFunc = false)
		=> new ReservedCompilerVariableSymbol("func" + func.Id.ToString(), "res", inlineFunc ? Modifiers.Inline : 0, func.Type);

	public static ReservedCompilerVariableSymbol CreateDiscard(TypeSymbol type)
		=> new ReservedCompilerVariableSymbol("discard", string.Empty, 0, type);

	public override ReservedCompilerVariableSymbol Clone()
		=> new ReservedCompilerVariableSymbol(Identifier, Name, Modifiers, Type);

	protected override string GetNameForResult()
		=> Identifier + "^" + Name;
}
