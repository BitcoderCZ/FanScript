﻿// <copyright file="StringExtensions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Utils;

internal static class StringExtensions
{
	public static IEnumerable<string> SplitByMaxLength(string str, int maxLength)
	{
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLength, 0);

		int index = 0;

		List<Range> currentLine = new List<Range>(maxLength / 2);
		int currentLineLength = 0;

		while (index < str.Length)
		{
			int spaceIndex = str.IndexOf(' ', index);

			if (spaceIndex == -1)
			{
				spaceIndex = str.Length;
			}

			Range wordRange = new Range(index, spaceIndex);
			int wordLength = spaceIndex - index;
			if (wordLength == 0)
			{
				goto setIndex;
			}

			if (currentLineLength + wordLength + 1 > maxLength)
			{ // + 1 -> the space between the words and the current word
				yield return CurrentLineToString();
				currentLine.Clear();
				currentLineLength = 0;
			}

			if (wordLength >= maxLength)
			{
				for (int i = 0; i < wordLength; i += maxLength)
				{
					int lengthToAdd = Math.Min(wordLength - i, maxLength);
					if (lengthToAdd == 0)
					{
						break;
					}

					currentLine.Add(new Range(index + i, index + i + lengthToAdd));
					AddLength(lengthToAdd);

					if (currentLineLength >= maxLength)
					{
						yield return CurrentLineToString();
						currentLine.Clear();
						currentLineLength = 0;
					}
				}
			}
			else
			{
				currentLine.Add(wordRange);
				AddLength(wordLength);
			}

		setIndex:
			index = spaceIndex + 1;
		}

		if (currentLineLength > 0)
		{
			yield return CurrentLineToString();
		}

		void AddLength(int length)
		{
			if (currentLineLength == 0)
			{
				currentLineLength = length;
			}
			else
			{
				currentLineLength += length + 1;
			}
		}

		string CurrentLineToString()
		{
			char[] line = new char[currentLineLength];

			int index = 0;

			for (int i = 0; i < currentLine.Count; i++)
			{
				Range range = currentLine[i];

				if (i != 0)
				{
					line[index++] = ' ';
				}

				var (offset, length) = range.GetOffsetAndLength(str.Length);

				str.CopyTo(offset, line, index, length);

				index += length;
			}

			return new string(line);
		}
	}

	public static int IndexOf(this ReadOnlySpan<char> str, char c, int indexNumb)
	{
		if (indexNumb == 0)
		{
			return str.IndexOf(c);
		}
		else if (indexNumb < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(indexNumb));
		}

		int removed = 0;
		int count = 0;
		int index;

		do
		{
			index = str.IndexOf(c);

			if (++count == indexNumb)
			{
				return index + removed;
			}

			str = str[(index + 1)..];
			removed += index + 1;
		} while (index != -1);

		return -1;
	}

	public static string ToUpperFirst(this string str)
		=> str.Length == 0
			? string.Empty
			: str.Length == 1 ? char.ToUpperInvariant(str[0]).ToString() : char.ToUpperInvariant(str[0]) + str[1..];

	public static string ToLowerFirst(this string str)
		=> str.Length == 0
			? string.Empty
			: str.Length == 1 ? char.ToLowerInvariant(str[0]).ToString() : char.ToLowerInvariant(str[0]) + str[1..];
}
