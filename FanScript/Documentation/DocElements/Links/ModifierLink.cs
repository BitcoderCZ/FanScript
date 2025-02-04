﻿// <copyright file="ModifierLink.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler;
using System.Collections.Immutable;
using System.Numerics;

namespace FanScript.Documentation.DocElements.Links;

public sealed class ModifierLink : DocLink
{
	public ModifierLink(ImmutableArray<DocArg> arguments, DocString value, Modifiers modifier)
		: base(arguments, value)
	{
		if (BitOperations.PopCount((uint)modifier) != 1)
		{
			throw new ArgumentException($"Only one flag in {nameof(modifier)} should be set.");
		}

		Modifier = modifier;
	}

	public Modifiers Modifier { get; }

	public override (string DisplayString, string LinkString) GetStrings()
		=> (Modifier.ToSyntaxString(), Enum.GetName(Modifier)!);
}
