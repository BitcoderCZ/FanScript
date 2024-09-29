namespace FanScript.Utils
{
    public static class StringExtensions
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
    }
}
