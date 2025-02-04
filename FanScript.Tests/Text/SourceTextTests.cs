﻿// <copyright file="SourceTextTests.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Text;

namespace FanScript.Tests.Text;

public class SourceTextTests
{
	[Theory]
	[InlineData(".", 1)]
	[InlineData(".\r\n", 2)]
	[InlineData(".\r\n\r\n", 3)]
	public void IncludesLastLine(string text, int expectedLineCount)
	{
		SourceText sourceText = SourceText.From(text);

		Assert.Equal(expectedLineCount, sourceText.Lines.Length);
	}
}
