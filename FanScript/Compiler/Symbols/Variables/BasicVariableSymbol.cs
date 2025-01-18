// <copyright file="BasicVariableSymbol.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Symbols.Variables;

public class BasicVariableSymbol : VariableSymbol
{
	internal BasicVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
		: base(name, modifiers, type)
	{
	}

	public override SymbolKind Kind => SymbolKind.BasicVariable;
}
