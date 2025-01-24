// <copyright file="ClassifiedSpan.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace FanScript.LangServer.Classification;

public readonly struct ClassifiedSpan
{
	public readonly TextSpan Span;
	public readonly SemanticTokenType Classification;

	public ClassifiedSpan(TextSpan span, SemanticTokenType classification)
	{
		Span = span;
		Classification = classification;
	}
}
