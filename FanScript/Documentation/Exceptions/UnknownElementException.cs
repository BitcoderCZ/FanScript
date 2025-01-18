// <copyright file="UnknownElementException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class UnknownElementException : DocParseException
{
	public UnknownElementException(string elementName)
		: base($"Unknown element \"{elementName}\".")
	{
	}
}
