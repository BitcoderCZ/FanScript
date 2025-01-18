// <copyright file="ElementArgMissingException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class ElementArgMissingException : DocParseException
{
	public ElementArgMissingException(string elementName, string argName)
		: base($"Required arg \"{argName}\" is missing in element \"{elementName}\".")
	{
	}
}
