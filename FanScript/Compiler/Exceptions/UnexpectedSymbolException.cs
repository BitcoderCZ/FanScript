// <copyright file="UnexpectedSymbolException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Exceptions;

public sealed class UnexpectedSymbolException : Exception
{
	public UnexpectedSymbolException(Symbol symbol)
		: base($"Unexpected symbol '{symbol?.GetType()?.FullName ?? "null"}'.")
	{
	}
}
