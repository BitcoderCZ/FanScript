// <copyright file="BoundLabel.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Binding;

internal sealed class BoundLabel
{
	internal BoundLabel(string name)
	{
		Name = name;
	}

	public string Name { get; }

	public override string ToString() => Name;
}
