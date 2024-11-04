using System.Buffers;
using System.Collections.Immutable;
using FanScript.Compiler;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Syntax;
using FanScript.Documentation.DocElements.Links;
using FanScript.Documentation.Exceptions;
using FanScript.Utils;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocElementParser
    {
        public readonly Dictionary<string, Func<ImmutableArray<DocArg>, DocElement?, DocElement>> ElementTypes;
        public readonly HashSet<string> ElementsWithRawValues;

        public DocElementParser(FunctionSymbol? currentFunction)
            : this(new DefaultValidator(currentFunction))
        {
        }

        public DocElementParser(IValidator validator)
        {
            Validator = validator;
            ElementTypes = new()
            {
                ["link"] = Validator.CreateAndValidateLink,
                ["list"] = (args, value) => new DocList(args, value),
                ["item"] = (args, value) => new DocList.Item(args, value),
                ["codeblock"] = (args, value) =>
                {
                    DocArg langArg = args.FirstOrDefault(arg => arg.Name == "lang");

                    string? lang = null;
                    if (langArg.Name is not null)
                    {
                        lang = string.IsNullOrEmpty(langArg.Value) ? throw new ElementArgValueMissingException("codeblock", "lang") : langArg.Value;
                    }

                    return value is not DocString valueStr
                        ? throw new ElementParseException("codeblock", "Value of a code block cannot be empty or not a string.")
                        : (DocElement)new DocCodeBlock(args, valueStr, lang);
                },
                ["header"] = (args, value) =>
                {
                    DocArg levelArg = args.FirstOrDefault(arg => arg.Name == "level");

                    if (levelArg.Name is null)
                    {
                        throw new ElementArgValueMissingException("header", "level");
                    }

                    if (!byte.TryParse(levelArg.Value, out byte level))
                    {
                        throw new ElementParseException("header", $"The level arg must be an int, is: '{levelArg.Value}'.");
                    }
                    else if (value is null)
                    {
                        throw new ElementParseException("header", "Value of a header cannot be empty.");
                    }

                    return new DocHeader(args, value, level);
                },
            };
            ElementsWithRawValues =
            [
                "codeblock"
            ];
        }

        public interface IValidator
        {
            DocLink CreateAndValidateLink(ImmutableArray<DocArg> arguments, DocElement? value);
        }

        public IValidator Validator { get; set; }

        public DocElement Parse(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return new DocBlock([]);
            }

            List<DocElement> elements = [];

            int index = 0;

            while (text.Length > 0 && index < text.Length)
            {
                if (text[index] == '<')
                {
                    if (index != 0)
                    {
                        elements.Add(new DocString(new string(text[..index])));
                    }

                    text = text[index..];
                    index = 0;

                    DocElement? element = ParseElement(ref text);
                    if (element is not null)
                    {
                        elements.Add(element);
                    }
                }
                else
                {
                    index++;
                }
            }

            if (text.Length > 0)
            {
                elements.Add(new DocString(new string(text)));
            }

            return elements.Count == 1 ? elements[0] : new DocBlock([.. elements]);
        }

        /// <summary>
        /// Parses the start tag (<{name} {args}>)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="UnclosedStartTagException"></exception>
        /// <exception cref="InvalidElementArgValueException"></exception>
        private static (string Name, ImmutableArray<DocArg> Arguments) ParseStartTag(ref ReadOnlySpan<char> text)
        {
            text = text[1..]; // skip the '<' char

            int index = 0;

            while (index < text.Length && text[index] != ' ' && text[index] != '>')
            {
                index++;
            }

            string elementName = new string(text[..index]);

            if (index >= text.Length)
            {
                throw new UnclosedStartTagException(elementName);
            }
            else if (text[index] == '>')
            {
                text = text[(index + 1)..];

                return (elementName, ImmutableArray<DocArg>.Empty);
            }

            text = text[(index + 1)..];

            List<DocArg> arguments = [];

            while (text.Length != 0 && index < text.Length && text[0] != '>')
            {
                index = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                while (index < text.Length && text[index] != ' ' && text[index] != '=' && text[index] != '>')
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    throw new UnclosedStartTagException(elementName);
                }

                string argName = new string(text[..index]);

                if (text[index++] != '=')
                {
                    text = text[..(index - 1)];
                    arguments.Add(new DocArg(argName, null));
                    continue;
                }

                if (text[index] != '"')
                {
                    throw new InvalidElementArgValueException(elementName, argName);
                }

                text = text[(index + 1)..];
                index = 0;

                while (index < text.Length && text[index] != '"')
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    throw new InvalidElementArgValueException(elementName, argName);
                }

                string argValue = new string(text[..index]);

                arguments.Add(new DocArg(argName, argValue));

                text = text[(index + 1)..];
            }

            if (text.Length == 0 || text[0] != '>')
            {
                throw new UnclosedStartTagException(elementName);
            }

            text = text[1..];

            return (elementName, arguments.ToImmutableArray());
        }

        /// <summary>
        /// Reads the end tag (</>)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="elementName"></param>
        /// <exception cref="UnclosedEndTagException"></exception>
        private static void ReadEndTag(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length < 3 || text[0] != '<' || text[1] != '/' || text[2] != '>')
            {
                throw new UnclosedEndTagException(elementName);
            }

            text = text[3..];
        }

        private static bool IsStartTag(ReadOnlySpan<char> text)
        {
            text = text[1..]; // skip the '<' char

            int index = 0;

            while (index < text.Length && text[index] != ' ' && text[index] != '>')
            {
                index++;
            }

            if (index >= text.Length)
            {
                return false;
            }
            else if (text[index] == '>')
            {
                return true;
            }

            text = text[(index + 1)..];

            while (text.Length != 0 && index < text.Length && text[0] != '>')
            {
                index = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                while (index < text.Length && text[index] != ' ' && text[index] != '=' && text[index] != '>')
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    return false;
                }

                if (text[index++] != '=')
                {
                    text = text[..(index - 1)];
                    continue;
                }

                if (text[index] != '"')
                {
                    return false;
                }

                text = text[(index + 1)..];
                index = 0;

                while (index < text.Length && text[index] != '"')
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    return false;
                }

                text = text[(index + 1)..];
            }

            return text.Length != 0 && text[0] == '>';
        }

        private DocElement? ParseElement(ref ReadOnlySpan<char> text)
        {
            var (name, arguments) = ParseStartTag(ref text);

            DocElement? value = ElementsWithRawValues.Contains(name)
                ? new DocString(new string(ReadElementValue(ref text, name)))
                : Parse(ReadElementValue(ref text, name));
            ReadEndTag(ref text, name);

            var duplicates = arguments.GetDuplicates();

            return duplicates.Any()
                ? throw new DuplicateElementArgException(name, duplicates.First().Name)
                : ElementTypes.TryGetValue(name, out var createFunc) ? createFunc(arguments, value) : throw new UnknownElementException(name);
        }

        /// <summary>
        /// Reads the value between <{name} {args}> and </> 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="elementName"></param>
        /// <returns></returns>
        /// <exception cref="UnclosedElementException"></exception>
        private ReadOnlySpan<char> ReadElementValue(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length == 0 || (text.Length >= 3 && text[0] == '<' && text[1] == '/' && text[2] == '>'))
            {
                return [];
            }

            bool rawValue = ElementsWithRawValues.Contains(elementName);

            int i;
            int depth = 0;
            for (i = 0; i < text.Length; i++)
            {
                if (text[i] == '<')
                {
                    if (text[++i] == '/')
                    {
                        if (depth == 0 || rawValue)
                        {
                            i--;
                            break;
                        }
                        else
                        {
                            depth--;
                        }
                    }
                    else if (IsStartTag(text[(i - 1)..]))
                    {
                        depth++;
                    }
                }
            }

            if (i >= text.Length)
            {
                throw new UnclosedElementException(elementName);
            }

            ReadOnlySpan<char> result = text[..i];

            text = text[i..];

            return result;
        }

        private readonly struct DefaultValidator : IValidator
        {
            public readonly FunctionSymbol? CurrentFunction;

            public DefaultValidator(FunctionSymbol? currentFunction)
            {
                CurrentFunction = currentFunction;
            }

            public DocLink CreateAndValidateLink(ImmutableArray<DocArg> arguments, DocElement? value)
            {
                DocArg type = arguments.FirstOrDefault(arg => arg.Name == "type");

                if (type.Name is null)
                {
                    throw new ElementArgMissingException("link", "type");
                }
                else if (string.IsNullOrEmpty(type.Value))
                {
                    throw new ElementArgValueMissingException("link", "type");
                }

                if (value is not DocString valString)
                {
                    throw new ElementParseException("link", "The value of a link must be a string.");
                }

                return type.Value switch
                {
                    "url" => CreateUrlLink(arguments, valString),
                    "func" => CreateFunctionLink(arguments, valString),
                    "param" => CreateParameterLink(arguments, valString),
                    "con" => CreateConstantLink(arguments, valString),
                    "con_value" => CreateConstantValueLink(arguments, valString),
                    "event" => CreateEventLink(arguments, valString),
                    "type" => CreateTypeLink(arguments, valString),
                    "mod" => CreateModifierLink(arguments, valString),
                    "build_command" => CreateBuildCommandLink(arguments, valString),
                    _ => throw new UnknownLinkTypeException(type.Value),
                };
            }

            private static UrlLink CreateUrlLink(ImmutableArray<DocArg> args, DocString value)
            {
                string[] split = value.Text.Split(';', StringSplitOptions.TrimEntries);

                return split.Length != 2
                    ? throw new ElementParseException("link", "The value of an url link must be in the format: '{display string};{url}'.")
                    : new UrlLink(args, value, split[0], split[1]);
            }

            private static ConstantLink CreateConstantLink(ImmutableArray<DocArg> args, DocString value)
            {
                ConstantGroup? group = Constants.Groups.First(group => group.Name == value.Text);

                return group is null
                    ? throw new ElementParseException("link", $"Constant \"{value.Text}\" doesn't exist.")
                    : new ConstantLink(args, value, group);
            }

            private static ConstantValueLink CreateConstantValueLink(ImmutableArray<DocArg> args, DocString value)
            {
                ConstantGroup? group = Constants.Groups.First(group => value.Text.StartsWith(group.Name));

                if (group is null || value.Text.Length < group.Name.Length)
                {
                    throw new ElementParseException("link", $"Constant value \"{value.Text}\" doesn't exist.");
                }

                var valName = value.Text.AsSpan(group.Name.Length + 1);

                Constant? constant = null;
                foreach (var val in group.Values)
                {
                    if (valName.Equals(val.Name, StringComparison.Ordinal))
                    {
                        constant = val;
                        break;
                    }
                }

                return constant is null
                    ? throw new ElementParseException("link", $"Constant value \"{value.Text}\" doesn't exist.")
                    : new ConstantValueLink(args, value, group, constant);
            }

            private static EventLink CreateEventLink(ImmutableArray<DocArg> args, DocString value)
                => Enum.TryParse(value.Text, out EventType eventType)
                    ? new EventLink(args, value, eventType)
                    : throw new ElementParseException("link", $"Event \"{value.Text}\" doesn't exist.");

            private static TypeLink CreateTypeLink(ImmutableArray<DocArg> args, DocString value)
            {
                TypeSymbol type = TypeSymbol.GetTypeInternal(value.Text);

                return new TypeLink(args, value, type);
            }

            private static ModifierLink CreateModifierLink(ImmutableArray<DocArg> args, DocString value)
            {
                SyntaxKind syntaxKind = SyntaxFacts.GetKeywordKind(value.Text);

                if (syntaxKind == SyntaxKind.IdentifierToken)
                {
                    throw new ElementParseException("link", $"Modifier \"{value.Text}\" doesn't exist.");
                }

                Modifiers modifier = ModifiersE.FromKind(syntaxKind);

                return new ModifierLink(args, value, modifier);
            }

            private static BuildCommandLink CreateBuildCommandLink(ImmutableArray<DocArg> args, DocString value)
                => Enum.TryParse(value.Text.ToUpperFirst(), out BuildCommand buildCommand)
                    ? new BuildCommandLink(args, value, buildCommand)
                    : throw new ElementParseException("link", $"Build command \"{value.Text}\" doesn't exist.");

            private FunctionLink CreateFunctionLink(ImmutableArray<DocArg> args, DocString value)
            {
                ReadOnlySpan<char> valSpan = value.Text.AsSpan();

                Span<Range> ranges = stackalloc Range[10]; // TODO: increase in case of a functions with more that 9 parameters
                int numbRanges = valSpan.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (numbRanges == 0)
                {
                    throw new ElementParseException("link", "Empty value.");
                }

                ReadOnlySpan<char> funcName = valSpan.Slice(ranges[0]);

                int numbParams = numbRanges - 1;

                TypeSymbol[] paramTypes;
                if (numbParams == 0)
                {
                    paramTypes = [];
                }
                else
                {
                    paramTypes = new TypeSymbol[numbParams];

                    for (int i = 0; i < numbParams; i++)
                    {
                        paramTypes[i] = TypeSymbol.GetTypeInternal(valSpan.Slice(ranges[i + 1]));
                    }
                }

                foreach (var func in BuiltinFunctions.GetAll())
                {
                    if (funcName.Equals(func.Name.AsSpan(), StringComparison.Ordinal) &&
                        func.Parameters.Length == numbParams &&
                        func.Parameters.Select(param => param.Type).SequenceEqual(paramTypes))
                    {
                        return new FunctionLink(args, value, func);
                    }
                }

                throw new ElementParseException("link", $"Functions \"{new string(funcName)}\" with parameter types: {string.Join(", ", paramTypes.Select(type => type.Name))}");
            }

            private ParamLink CreateParameterLink(ImmutableArray<DocArg> args, DocString value)
            {
                if (CurrentFunction is null)
                {
                    throw new ElementParseException("link", $"Param link cannot be used when {nameof(CurrentFunction)} is null.");
                }

                string paramName = value.Text;

                int paramIndex = Array.IndexOf(
                    CurrentFunction.Parameters
                        .Select(param => param.Name)
                        .ToArray(),
                    paramName);

                return paramIndex < 0
                    ? throw new ElementParseException("link", $"Function \"{CurrentFunction.Name}\" doesn't have a parameter \"{paramName}\".")
                    : new ParamLink(args, value, CurrentFunction, paramIndex);
            }
        }
    }
}
