﻿// <copyright file="DocElement.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements;

public abstract class DocElement
{
	protected DocElement(ImmutableArray<DocArg> arguments, DocElement? value)
	{
		Arguments = arguments;
		Value = value;
	}

	public ImmutableArray<DocArg> Arguments { get; }

	public virtual DocElement? Value { get; }
}
