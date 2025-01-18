// <copyright file="ElementArgValueMissingException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class ElementArgValueMissingException : DocParseException
{
	public ElementArgValueMissingException(string elementName, string argName)
		: base($"Required value of arg \"{argName}\" in element \"{elementName}\" is missing.")
	{
	}
}
