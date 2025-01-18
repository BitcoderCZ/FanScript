// <copyright file="TextLocation.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Text;

public readonly struct TextLocation
{
	public static readonly TextLocation None = new TextLocation(null, default);

	public TextLocation(SourceText? text, TextSpan span)
	{
		Text = text;
		Span = span;
	}

	public SourceText? Text { get; }

	public TextSpan Span { get; }

	public readonly string FileName => Text!.FileName;

	public int StartLine => Text!.GetLineIndex(Span.Start);

	public int StartCharacter => Span.Start - Text!.Lines[StartLine].Start;

	public int EndLine => Text!.GetLineIndex(Span.End);

	public int EndCharacter => Span.End - Text!.Lines[EndLine].Start;

	public override string ToString()
		=> $"{StartLine},{StartCharacter}..{EndLine},{EndCharacter} ({Span})";
}
