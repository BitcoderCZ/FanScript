// <copyright file="DocUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Functions;
using FanScript.Documentation.Attributes;
using FanScript.Documentation.DocElements;
using FanScript.Documentation.DocElements.Builders;
using System;
using System.Linq;

namespace FanScript.LangServer.Utils;

internal static class DocUtils
{
	private static readonly DocElementParser Parser = new DocElementParser((FunctionSymbol?)null);
	private static readonly DocElementBuilder Builder = new TextBuilder();

	public static string ParseAndBuild(ReadOnlySpan<char> text, FunctionSymbol? currentFunction)
		=> Build(Parse(text, currentFunction));

	public static DocElement Parse(ReadOnlySpan<char> text, FunctionSymbol? currentFunction)
		=> currentFunction is null ? Parser.Parse(text) : new DocElementParser(currentFunction).Parse(text);

	public static string Build(DocElement? element)
		=> Builder.Build(element);

	public static TAttrib GetAttribute<TEnum, TAttrib>(TEnum value)
	   where TEnum : Enum
	   where TAttrib : DocumentationAttribute
	{
		Type enumType = typeof(TEnum);
		string name = Enum.GetName(enumType, value)!;
		return enumType
			.GetField(name)!
			.GetCustomAttributes(false)
			.OfType<TAttrib>()
			.Single();
	}
}
