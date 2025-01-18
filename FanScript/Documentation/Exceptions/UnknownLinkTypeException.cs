// <copyright file="UnknownLinkTypeException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public sealed class UnknownLinkTypeException : DocParseException
{
	public UnknownLinkTypeException(string type)
		: base($"Unknown link type \"{type}\".")
	{
	}
}
