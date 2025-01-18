// <copyright file="UnclosedElementException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class UnclosedElementException : DocParseException
{
	public UnclosedElementException(string elementName)
		: base($"Element \"{elementName}\" isn't closed.")
	{
	}
}
