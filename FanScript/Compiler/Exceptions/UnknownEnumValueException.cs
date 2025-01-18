// <copyright file="UnknownEnumValueException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Exceptions;

public sealed class UnknownEnumValueException<T> : Exception
	where T : struct, Enum
{
	public UnknownEnumValueException(T value)
		: base($"Unknown enum value '{typeof(T).Name}.{value}'.")
	{
	}

	public UnknownEnumValueException(T? value)
		: base($"Unknown enum value '{(value is null ? "null" : typeof(T).Name + "." + value)}'.")
	{
	}
}
