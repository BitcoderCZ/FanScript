// <copyright file="ElementParseException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class ElementParseException : DocParseException
{
	public ElementParseException(string elementType, string message)
		: base($"Error parsing element \"{elementType}\": " + message)
	{
	}
}
