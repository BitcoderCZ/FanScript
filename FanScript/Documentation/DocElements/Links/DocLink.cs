// <copyright file="DocLink.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements.Links;

public abstract class DocLink : DocElement
{
	protected DocLink(ImmutableArray<DocArg> arguments, DocString value)
		: base(arguments, value)
	{
	}

	public abstract (string DisplayString, string LinkString) GetStrings();
}
