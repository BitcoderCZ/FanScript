// <copyright file="DocString.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.DocElements;

public sealed class DocString : DocElement
{
	public DocString(string text)
		: base([], null)
	{
		Text = text;
	}

	public string Text { get; }
}
