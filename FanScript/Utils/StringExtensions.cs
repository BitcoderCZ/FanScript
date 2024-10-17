namespace FanScript.Utils
{
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
                    spaceIndex = str.Length;

                Range wordRange = new Range(index, spaceIndex);
                int wordLength = spaceIndex - index;
                if (wordLength == 0)
                    goto setIndex;

                if (currentLineLength + wordLength + 1 > maxLength)
                { // + 1 -> the space between the words and the current word
                    yield return currentLineToString();
                    currentLine.Clear();
                    currentLineLength = 0;
                }

                if (wordLength >= maxLength)
                {
                    for (int i = 0; i < wordLength; i += maxLength)
                    {
                        int lengthToAdd = Math.Min(wordLength - i, maxLength);
                        if (lengthToAdd == 0)
                            break;

                        currentLine.Add(new Range(index + i, index + i + lengthToAdd));
                        addLength(lengthToAdd);

                        if (currentLineLength >= maxLength)
                        {
                            yield return currentLineToString();
                            currentLine.Clear();
                            currentLineLength = 0;
                        }
                    }
                }
                else
                {
                    currentLine.Add(wordRange);
                    addLength(wordLength);
                }

            setIndex:
                index = spaceIndex + 1;
            }

            if (currentLineLength > 0)
                yield return currentLineToString();

            void addLength(int length)
            {
                if (currentLineLength == 0)
                    currentLineLength = length;
                else
                    currentLineLength += length + 1;
            }

            string currentLineToString()
            {
                char[] line = new char[currentLineLength];

                int index = 0;

                for (int i = 0; i < currentLine.Count; i++)
                {
                    Range range = currentLine[i];

                    if (i != 0)
                        line[index++] = ' ';

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
                return str.IndexOf(c);
            else if (indexNumb < 0)
                throw new ArgumentOutOfRangeException(nameof(indexNumb));

            int index = -1;
            int removed = 0;
            int count = 0;

            do
            {
                index = str.IndexOf(c);

                if (++count == indexNumb)
                    return index + removed;

                str = str.Slice(index + 1);
                removed += index + 1;
            } while (index != -1);

            return -1;
        }

        public static string ToUpperFirst(this string str)
        {
            if (str.Length == 0)
                return string.Empty;
            else if (str.Length == 1)
                return char.ToUpperInvariant(str[0]).ToString();
            else
                return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        public static string ToLowerFirst(this string str)
        {
            if (str.Length == 0)
                return string.Empty;
            else if (str.Length == 1)
                return char.ToLowerInvariant(str[0]).ToString();
            else
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }
}
