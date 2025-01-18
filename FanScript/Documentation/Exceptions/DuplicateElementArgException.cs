// <copyright file="DuplicateElementArgException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class DuplicateElementArgException : DocParseException
{
	public DuplicateElementArgException(string elementName, string argName)
		: base($"Arg \"{argName}\" is multiple times in element \"{elementName}\".")
	{
	}
}
