using FanScript.Compiler.Text;
using System.Collections.Immutable;
using System.Text;

namespace FanScript.Tests;

internal sealed class AnnotatedText
{
	public AnnotatedText(string text, ImmutableArray<TextSpan> spans)
	{
		Text = text;
		Spans = spans;
	}

	public string Text { get; }
	public ImmutableArray<TextSpan> Spans { get; }

	public static AnnotatedText Parse(string text, bool noLocationDiagnostic)
	{
		text = Unindent(text);

		var textBuilder = new StringBuilder();
		var spanBuilder = ImmutableArray.CreateBuilder<TextSpan>();
		var startStack = new Stack<int>();

		if (noLocationDiagnostic)
			spanBuilder.Add(default);

		int position = 0;

		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			char nextC = text[i + 1 < text.Length ? i + 1 : text.Length - 1];

			if (c == '$' && nextC == '[')
			{
				startStack.Push(position);
				i++;
			}
			else if (c == ']' && nextC == '$')
			{
				if (startStack.Count == 0)
					throw new ArgumentException("Too many ']$' in text", nameof(text));

				int start = startStack.Pop();
				int end = position;
				TextSpan span = TextSpan.FromBounds(start, end);
				spanBuilder.Add(span);
				i++;
			}
			else
			{
				position++;
				textBuilder.Append(c);
			}
		}

		return startStack.Count != 0
			? throw new ArgumentException("Missing ']$' in text", nameof(text))
			: new AnnotatedText(textBuilder.ToString(), spanBuilder.ToImmutable());
	}

	private static string Unindent(string text)
		=> string.Join(Environment.NewLine, UnindentLines(text));

	public static string[] UnindentLines(string text)
	{
		var lines = new List<string>();

		using (var reader = new StringReader(text))
		{
			string? line;
			while ((line = reader.ReadLine()) != null)
				lines.Add(line);
		}

		int minIndentation = int.MaxValue;
		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];

			if (line.Trim().Length == 0)
			{
				lines[i] = string.Empty;
				continue;
			}

			int indentation = line.Length - line.TrimStart().Length;
			minIndentation = Math.Min(minIndentation, indentation);
		}

		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i].Length == 0)
				continue;

			lines[i] = lines[i][minIndentation..];
		}

		while (lines.Count > 0 && lines[0].Length == 0)
			lines.RemoveAt(0);

		while (lines.Count > 0 && lines[^1].Length == 0)
			lines.RemoveAt(lines.Count - 1);

		return [.. lines];
	}
}
