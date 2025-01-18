// <copyright file="TextWriterExtensions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;

namespace FanScript.Utils;

public static class TextWriterExtensions
{
	public static bool IsConsole(this TextWriter writer)
	{
		if (writer == Console.Out)
		{
			return !Console.IsOutputRedirected;
		}
		else if (writer == Console.Error)
		{
			return !Console.IsErrorRedirected && !Console.IsOutputRedirected; // Color codes are always output to Console.Out
		}
		else
		{
			return writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole();
		}
	}

	public static void SetForeground(this TextWriter writer, ConsoleColor color)
	{
		if (writer.IsConsole())
		{
			Console.ForegroundColor = color;
		}
	}

	public static void ResetColor(this TextWriter writer)
	{
		if (writer.IsConsole())
		{
			Console.ResetColor();
		}
	}

	public static void WriteWritable(this TextWriter writer, ITextWritable value)
		=> value.WriteTo(writer);

	public static void WriteKeyword(this TextWriter writer, SyntaxKind kind)
	{
		string? text = SyntaxFacts.GetText(kind);
		Debug.Assert(kind.IsKeyword() && text is not null, $"{nameof(kind)} must be a keyword and it's text cannot be null.");

		writer.WriteKeyword(text);
	}

	public static void WriteModifiers(this TextWriter writer, Modifiers modifiers)
	{
		bool isFirst = true;

		foreach (var modifier in Enum.GetValues<Modifiers>())
		{
			if (modifiers.HasFlag(modifier))
			{
				if (!isFirst)
				{
					writer.WriteSpace();
				}

				isFirst = false;

				writer.WriteKeyword(modifier.ToKind().GetText());
			}
		}
	}

	public static void WriteKeyword(this TextWriter writer, string? text)
	{
		writer.SetForeground(ConsoleColor.Blue);
		writer.Write(text);
		writer.ResetColor();
	}

	public static void WriteIdentifier(this TextWriter writer, string? text)
	{
		writer.SetForeground(ConsoleColor.DarkYellow);
		writer.Write(text ?? "null");
		writer.ResetColor();
	}

	public static void WriteNumber(this TextWriter writer, float value)
		=> WriteNumber(writer, value.ToString(CultureInfo.InvariantCulture));

	public static void WriteNumber(this TextWriter writer, string text)
	{
		writer.SetForeground(ConsoleColor.Cyan);
		writer.Write(text);
		writer.ResetColor();
	}

	public static void WriteString(this TextWriter writer, string text)
	{
		writer.SetForeground(ConsoleColor.Magenta);
		writer.Write(text);
		writer.ResetColor();
	}

	public static void WriteSpace(this TextWriter writer)
		=> writer.WritePunctuation(" ");

	public static void WritePunctuation(this TextWriter writer, SyntaxKind kind)
	{
		string? text = SyntaxFacts.GetText(kind);
		Debug.Assert(text is not null, $"{nameof(kind)}'s text cannot be null.");

		writer.WritePunctuation(text);
	}

	public static void WritePunctuation(this TextWriter writer, string text)
	{
		writer.SetForeground(ConsoleColor.DarkGray);
		writer.Write(text);
		writer.ResetColor();
	}

	public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
	{
		foreach (Diagnostic? diagnostic in diagnostics
			.Where(d => d.Location.Text is null))
		{
			ConsoleColor messageColor = diagnostic.IsWarning ? ConsoleColor.DarkYellow : ConsoleColor.DarkRed;
			writer.SetForeground(messageColor);
			writer.WriteLine(diagnostic.Message);
			writer.ResetColor();
		}

		foreach (Diagnostic? diagnostic in diagnostics
			.Where(d => d.Location.Text is not null)
			.OrderBy(d => d.Location.FileName)
			.ThenBy(d => d.Location.Span.Start)
			.ThenBy(d => d.Location.Span.Length))
		{
			SourceText text = diagnostic.Location.Text!;
			string fileName = diagnostic.Location.FileName;
			int startLineIndex = diagnostic.Location.StartLine + 1;
			int startCharacter = diagnostic.Location.StartCharacter + 1;
			int endLineIndex = diagnostic.Location.EndLine + 1;
			int endCharacter = diagnostic.Location.EndCharacter + 1;

			TextSpan span = diagnostic.Location.Span;
			TextLine startLine = text.Lines[text.GetLineIndex(span.Start)];
			TextLine endLine = text.Lines[text.GetLineIndex(span.End)];

			writer.WriteLine();

			ConsoleColor messageColor = diagnostic.IsWarning ? ConsoleColor.DarkYellow : ConsoleColor.DarkRed;
			writer.SetForeground(messageColor);
			writer.Write($"{fileName}({startLineIndex},{startCharacter},{endLineIndex},{endCharacter}): ");
			writer.WriteLine(diagnostic);
			writer.ResetColor();

			TextSpan prefixSpan = TextSpan.FromBounds(startLine.Start, span.Start);
			TextSpan suffixSpan = TextSpan.FromBounds(span.End, endLine.End);

			string prefix = text.ToString(prefixSpan);
			string error = text.ToString(span);
			string suffix = text.ToString(suffixSpan);

			writer.Write("    ");
			writer.Write(prefix);

			writer.SetForeground(ConsoleColor.DarkRed);
			writer.Write(error);
			writer.ResetColor();

			writer.Write(suffix);

			writer.WriteLine();
		}

		writer.WriteLine();
	}
}
