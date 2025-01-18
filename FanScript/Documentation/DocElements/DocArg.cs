// <copyright file="DocArg.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.DocElements;

public readonly struct DocArg
{
	public readonly string Name;
	public readonly string? Value;

	public DocArg(string name, string? value)
	{
		Name = name;
		Value = value;
	}
}
