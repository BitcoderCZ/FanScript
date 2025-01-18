// <copyright file="DocParseException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Exceptions;

public abstract class DocParseException : Exception
{
	protected DocParseException(string message)
		: base(message)
	{
	}
}
