using FanScript.Compiler.Symbols;
using FanScript.Documentation.Exceptions;
using FanScript.Utils;
using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public static class DocElementParser
    {
        public static DocElement Parse(ReadOnlySpan<char> text, IValidator validator)
        {
            if (text.IsEmpty)
                return new DocBlock(ImmutableArray<DocElement>.Empty);

            List<DocElement> elements = new();

            int index = 0;

            while (text.Length > 0 && index < text.Length)
            {
                if (text[index] == '<')
                {
                    if (index != 0)
                        elements.Add(new DocString(new string(text.Slice(0, index))));

                    text = text.Slice(index);

                    DocElement? element = parseElement(ref text, validator);
                    if (element is not null)
                        elements.Add(element);
                }
            }

            if (elements.Count == 1)
                return elements[0];
            else
                return new DocBlock(elements.ToImmutableArray());
        }

        private static DocElement? parseElement(ref ReadOnlySpan<char> text, IValidator validator)
        {
            var (name, arguments) = parseStartTag(ref text);
            DocElement? value = Parse(parseElementValue(ref text, name), validator);
            readEndTag(ref text, name);

            var duplicates = arguments.GetDuplicates();

            if (duplicates.Any())
                throw new DuplicateElementArgException(name, duplicates.First().Name);

            switch (name)
            {
                case "link":
                    return validator.CreateLink(arguments, value);
                case "list":
                    return new DocList(arguments, value);
                case "item":
                    return new DocListItem(arguments, value);
                default:
                    throw new UnknownElementException(name);
            }
        }

        private static (string Name, ImmutableArray<DocArg> Arguments) parseStartTag(ref ReadOnlySpan<char> text)
        {
            text = text.Slice(1); // skip the '<' char

            int index = 0;

            while (index < text.Length && text[index] != ' ' && text[index] != '>')
                index++;

            string elementName = new string(text.Slice(0, index));

            if (index >= text.Length)
                throw new UnclosedStartTagException(elementName);
            else if (text[index] == '>')
            {
                text = text.Slice(index + 1);

                return (elementName, ImmutableArray<DocArg>.Empty);
            }

            text = text.Slice(index + 1);
            index = 0;

            List<DocArg> arguments = new();

            while (text.Length != 0 && index < text.Length)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                while (index < text.Length && text[index] != ' ' && text[index] != '=' && text[index] != '>')
                    index++;

                if (index >= text.Length)
                    throw new UnclosedStartTagException(elementName);

                string argName = new string(text.Slice(0, index));

                if (text[index++] != '=')
                {
                    arguments.Add(new DocArg(argName, null));
                    continue;
                }

                if (text[index] != '"')
                    throw new InvalidElementArgValueException(elementName, argName);

                text = text.Slice(index + 1);
                index = 0;

                while (index < text.Length && text[index] != '"')
                    index++;

                if (index >= text.Length)
                    throw new InvalidElementArgValueException(elementName, argName);

                string argValue = new string(text.Slice(0, index));

                text = text.Slice(index + 1);
            }

            if (text.Length == 0 || text[0] != '>')
                throw new UnclosedStartTagException(elementName);

            text = text.Slice(1);

            return (elementName, arguments.ToImmutableArray());
        }

        private static ReadOnlySpan<char> parseElementValue(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length == 0 || text[0] == '<')
                return ReadOnlySpan<char>.Empty;

            int i;
            int depth = 0;
            for (i = 0; i < text.Length; i++)
            {
                if (text[i++] == '<')
                {
                    if (text[i++] == '/')
                    {
                        if (depth == 0)
                        {
                            i -= 2;
                            break;
                        }
                    }
                    else
                        depth++;
                }
            }

            if (i >= text.Length)
                throw new UnclosedElementException(elementName);

            ReadOnlySpan<char> result = text.Slice(0, i);

            text = text.Slice(i);

            return result;
        }

        private static void readEndTag(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length < 3 || text[0] != '<' || text[1] != '/' || text[2] != '>')
                throw new UnclosedEndTagException(elementName);

            text = text.Slice(3);
        }

        public interface IValidator
        {
            DocLink CreateLink(ImmutableArray<DocArg> arguments, DocElement? value);
        }

        public class DefaultValidator : IValidator
        {
            public DefaultValidator(FunctionSymbol? currentFunction)
            {
                CurrentFunction = currentFunction;
            }

            public FunctionSymbol? CurrentFunction { get; }

            public DocLink CreateLink(ImmutableArray<DocArg> arguments, DocElement? value)
            {
                DocArg? _type = arguments.FirstOrDefault(arg => arg.Name == "type");

                if (_type is not DocArg type)
                    throw new ElementArgMissingException("link", "type");
                else if (string.IsNullOrEmpty(type.Value))
                    throw new ElementArgValueMissingException("link", "type");

                switch (type.Value)
                {
                    case "func":
                        return createFunctionLink(value);
                    case "param":
                    case "con":
                    case "con_value":
                    default:
                        throw new UnknownLinkTypeException(type.Value);
                }
            }

            private FunctionLink createFunctionLink(DocElement? value)
            {

            }
        }
    }
}
