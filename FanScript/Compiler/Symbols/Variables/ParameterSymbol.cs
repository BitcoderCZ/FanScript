// <copyright file="ParameterSymbol.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Utils;

namespace FanScript.Compiler.Symbols.Variables;

public sealed class ParameterSymbol : BasicVariableSymbol
{
	internal ParameterSymbol(string name, TypeSymbol type)
		: this(name, 0, type)
	{
	}

	internal ParameterSymbol(string name, Modifiers modifiers, TypeSymbol type)
		: base(name, modifiers, type)
	{
		Initialize(null);
	}

	public override SymbolKind Kind => SymbolKind.Parameter;

	public override void WriteTo(TextWriter writer)
	{
		if (Modifiers != 0)
		{
			writer.WriteModifiers(Modifiers);
			writer.WriteSpace();
		}

		writer.WriteWritable(Type);
		writer.WriteSpace();
		writer.WriteIdentifier(Name);
	}
}
