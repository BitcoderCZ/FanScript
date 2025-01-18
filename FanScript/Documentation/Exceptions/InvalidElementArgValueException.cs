// <copyright file="InvalidElementArgValueException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class InvalidElementArgValueException : DocParseException
{
	public InvalidElementArgValueException(string elementName, string argName)
		: base($"Arg \"{argName}\" in element \"{elementName}\" has invalid value.")
	{
	}
}
