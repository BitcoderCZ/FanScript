// <copyright file="ParamLink.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Functions;
using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements.Links;

public sealed class ParamLink : DocLink
{
	public ParamLink(ImmutableArray<DocArg> arguments, DocString value, FunctionSymbol function, int paramIndex)
		: base(arguments, value)
	{
		if (paramIndex < 0 || paramIndex >= function.Parameters.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(paramIndex));
		}

		Function = function;
		ParamIndex = paramIndex;
	}

	public FunctionSymbol Function { get; }

	public int ParamIndex { get; }

	public string ParamName => Function.Parameters[ParamIndex].Name;

	public override (string DisplayString, string LinkString) GetStrings()
		=> (ParamName, ParamName);
}
