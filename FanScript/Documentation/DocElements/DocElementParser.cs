﻿using FanScript.Compiler;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Documentation.DocElements.Links;
using FanScript.Documentation.Exceptions;
using FanScript.Utils;
using System.Buffers;
using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements
{
    public sealed class DocElementParser
    {
        public readonly Dictionary<string, Func<ImmutableArray<DocArg>, DocElement?, DocElement>> ElementTypes;
        public readonly HashSet<string> ElementsWithRawValues;
        public IValidator Validator { get; set; }

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
                ["codeblock"] = (args, _value) =>
                {
                    DocArg _lang = args.FirstOrDefault(arg => arg.Name == "lang");

                    string? lang = null;
                    if (_lang.Name is not null)
                    {
                        if (string.IsNullOrEmpty(_lang.Value))
                            throw new ElementArgValueMissingException("codeblock", "lang");
                        else
                            lang = _lang.Value;
                    }

                    if (_value is not DocString valueStr)
                        throw new ElementParseException("codeblock", "Value of a code block cannot be empty or not a string.");

                    return new DocCodeBlock(args, valueStr, lang);
                },
                ["header"] = (args, value) =>
                {
                    DocArg levelArg = args.FirstOrDefault(arg => arg.Name == "level");

                    if (levelArg.Name is null)
                        throw new ElementArgValueMissingException("header", "level");

                    if (!byte.TryParse(levelArg.Value, out byte level))
                        throw new ElementParseException("header", $"The level arg must be an int, is: '{levelArg.Value}'.");


                    if (value is null)
                        throw new ElementParseException("header", "Value of a header cannot be empty.");

                    return new DocHeader(args, value, level);
                }
            };
            ElementsWithRawValues =
            [
                "codeblock"
            ];
        }

        public DocElement Parse(ReadOnlySpan<char> text)
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
                    index = 0;

                    DocElement? element = parseElement(ref text);
                    if (element is not null)
                        elements.Add(element);
                }
                else
                    index++;
            }

            if (text.Length > 0)
                elements.Add(new DocString(new string(text)));

            if (elements.Count == 1)
                return elements[0];
            else
                return new DocBlock(elements.ToImmutableArray());
        }

        private DocElement? parseElement(ref ReadOnlySpan<char> text)
        {
            var (name, arguments) = parseStartTag(ref text);

            DocElement? value;
            if (ElementsWithRawValues.Contains(name))
                value = new DocString(new string(readElementValue(ref text, name)));
            else
                value = Parse(readElementValue(ref text, name));

            readEndTag(ref text, name);

            var duplicates = arguments.GetDuplicates();

            if (duplicates.Any())
                throw new DuplicateElementArgException(name, duplicates.First().Name);

            if (ElementTypes.TryGetValue(name, out var createFunc))
                return createFunc(arguments, value);
            else
                throw new UnknownElementException(name);
        }

        /// <summary>
        /// Parses the start tag (<{name} {args}>)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="UnclosedStartTagException"></exception>
        /// <exception cref="InvalidElementArgValueException"></exception>
        private (string Name, ImmutableArray<DocArg> Arguments) parseStartTag(ref ReadOnlySpan<char> text)
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

            List<DocArg> arguments = new();

            while (text.Length != 0 && index < text.Length && text[0] != '>')
            {
                index = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                while (index < text.Length && text[index] != ' ' && text[index] != '=' && text[index] != '>')
                    index++;

                if (index >= text.Length)
                    throw new UnclosedStartTagException(elementName);

                string argName = new string(text.Slice(0, index));

                if (text[index++] != '=')
                {
                    text = text.Slice(0, index - 1);
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

                arguments.Add(new DocArg(argName, argValue));

                text = text.Slice(index + 1);
            }

            if (text.Length == 0 || text[0] != '>')
                throw new UnclosedStartTagException(elementName);

            text = text.Slice(1);

            return (elementName, arguments.ToImmutableArray());
        }

        private bool isStartTag(ReadOnlySpan<char> text)
        {
            text = text.Slice(1); // skip the '<' char

            int index = 0;

            while (index < text.Length && text[index] != ' ' && text[index] != '>')
                index++;

            string elementName = new string(text.Slice(0, index));

            if (index >= text.Length)
                return false;
            else if (text[index] == '>')
                return true;

            text = text.Slice(index + 1);

            while (text.Length != 0 && index < text.Length && text[0] != '>')
            {
                index = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                while (index < text.Length && text[index] != ' ' && text[index] != '=' && text[index] != '>')
                    index++;

                if (index >= text.Length)
                    return false;

                if (text[index++] != '=')
                {
                    text = text.Slice(0, index - 1);
                    continue;
                }

                if (text[index] != '"')
                    return false;

                text = text.Slice(index + 1);
                index = 0;

                while (index < text.Length && text[index] != '"')
                    index++;

                if (index >= text.Length)
                    return false;

                text = text.Slice(index + 1);
            }

            if (text.Length == 0 || text[0] != '>')
                return false;

            return true;
        }

        /// <summary>
        /// Reads the value between <{name} {args}> and </> 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="elementName"></param>
        /// <returns></returns>
        /// <exception cref="UnclosedElementException"></exception>
        private ReadOnlySpan<char> readElementValue(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length == 0 || (text.Length >= 3 && text[0] == '<' && text[1] == '/' && text[2] == '>'))
                return ReadOnlySpan<char>.Empty;

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
                            depth--;
                    }
                    else if (isStartTag(text.Slice(i - 1)))
                        depth++;
                }
            }

            if (i >= text.Length)
                throw new UnclosedElementException(elementName);

            ReadOnlySpan<char> result = text.Slice(0, i);

            text = text.Slice(i);

            return result;
        }

        /// <summary>
        /// Reads the end tag (</>)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="elementName"></param>
        /// <exception cref="UnclosedEndTagException"></exception>
        private void readEndTag(ref ReadOnlySpan<char> text, string elementName)
        {
            if (text.Length < 3 || text[0] != '<' || text[1] != '/' || text[2] != '>')
                throw new UnclosedEndTagException(elementName);

            text = text.Slice(3);
        }

        public interface IValidator
        {
            DocLink CreateAndValidateLink(ImmutableArray<DocArg> arguments, DocElement? value);
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
                    throw new ElementArgMissingException("link", "type");
                else if (string.IsNullOrEmpty(type.Value))
                    throw new ElementArgValueMissingException("link", "type");
                if (value is not DocString valString)
                    throw new ElementParseException("link", "The value of a link must be a string.");

                switch (type.Value)
                {
                    case "url":
                        return createUrlLink(arguments, valString);
                    case "func":
                        return createFunctionLink(arguments, valString);
                    case "param":
                        return createParameterLink(arguments, valString);
                    case "con":
                        return createConstantLink(arguments, valString);
                    case "con_value":
                        return createConstantValueLink(arguments, valString);
                    case "event":
                        return createEventLink(arguments, valString);
                    case "type":
                        return createTypeLink(arguments, valString);
                    case "mod":
                        return createModifierLink(arguments, valString);
                    case "build_command":
                        return createBuildCommandLink(arguments, valString);
                    default:
                        throw new UnknownLinkTypeException(type.Value);
                }
            }

            private UrlLink createUrlLink(ImmutableArray<DocArg> args, DocString value)
            {
                string[] split = value.Text.Split(';', StringSplitOptions.TrimEntries);

                if (split.Length != 2)
                    throw new ElementParseException("link", "The value of an url link must be in the format: '{display string};{url}'.");

                return new UrlLink(args, value, split[0], split[1]);
            }

            private FunctionLink createFunctionLink(ImmutableArray<DocArg> args, DocString value)
            {
                ReadOnlySpan<char> valSpan = value.Text.AsSpan();

                Span<Range> ranges = stackalloc Range[10]; // TODO: increase in case of a functions with more that 9 parameters
                int numbRanges = valSpan.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (numbRanges == 0)
                    throw new ElementParseException("link", "Empty value.");

                ReadOnlySpan<char> funcName = valSpan.Slice(ranges[0]);

                int numbParams = numbRanges - 1;

                TypeSymbol[] paramTypes;
                if (numbParams == 0)
                    paramTypes = Array.Empty<TypeSymbol>();
                else
                {
                    paramTypes = new TypeSymbol[numbParams];

                    for (int i = 0; i < numbParams; i++)
                        paramTypes[i] = TypeSymbol.GetTypeInternal(valSpan.Slice(ranges[i + 1]));
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

            private ParamLink createParameterLink(ImmutableArray<DocArg> args, DocString value)
            {
                if (CurrentFunction is null)
                    throw new ElementParseException("link", $"Param link cannot be used when {nameof(CurrentFunction)} is null.");

                string paramName = value.Text;

                int paramIndex = Array.IndexOf(
                    CurrentFunction.Parameters
                        .Select(param => param.Name)
                        .ToArray(),
                    paramName
                );

                if (paramIndex < 0)
                    throw new ElementParseException("link", $"Function \"{CurrentFunction.Name}\" doesn't have a parameter \"{paramName}\".");

                return new ParamLink(args, value, CurrentFunction, paramIndex);
            }

            private ConstantLink createConstantLink(ImmutableArray<DocArg> args, DocString value)
            {
                ConstantGroup? group = Constants.Groups.First(group => group.Name == value.Text);

                if (group is null)
                    throw new ElementParseException("link", $"Constant \"{value.Text}\" doesn't exist.");

                return new ConstantLink(args, value, group);
            }

            private ConstantValueLink createConstantValueLink(ImmutableArray<DocArg> args, DocString value)
            {
                ConstantGroup? group = Constants.Groups.First(group => value.Text.StartsWith(group.Name));

                if (group is null || value.Text.Length < group.Name.Length)
                    throw new ElementParseException("link", $"Constant value \"{value.Text}\" doesn't exist.");

                string valName = value.Text.Substring(group.Name.Length + 1);

                Constant? constant = null;
                foreach (var val in group.Values)
                {
                    if (val.Name == valName)
                    {
                        constant = val;
                        break;
                    }
                }

                if (constant is null)
                    throw new ElementParseException("link", $"Constant value \"{value.Text}\" doesn't exist.");

                return new ConstantValueLink(args, value, group, constant);
            }

            private EventLink createEventLink(ImmutableArray<DocArg> args, DocString value)
            {
                if (!Enum.TryParse(value.Text, out EventType eventType))
                    throw new ElementParseException("link", $"Event \"{value.Text}\" doesn't exist.");

                return new EventLink(args, value, eventType);
            }

            private TypeLink createTypeLink(ImmutableArray<DocArg> args, DocString value)
            {
                TypeSymbol type = TypeSymbol.GetTypeInternal(value.Text);

                return new TypeLink(args, value, type);
            }

            private ModifierLink createModifierLink(ImmutableArray<DocArg> args, DocString value)
            {
                SyntaxKind syntaxKind = SyntaxFacts.GetKeywordKind(value.Text);

                if (syntaxKind == SyntaxKind.IdentifierToken)
                    throw new ElementParseException("link", $"Modifier \"{value.Text}\" doesn't exist.");

                Modifiers modifier = ModifiersE.FromKind(syntaxKind);

                return new ModifierLink(args, value, modifier);
            }

            private BuildCommandLink createBuildCommandLink(ImmutableArray<DocArg> args, DocString value)
            {
                if (!Enum.TryParse(value.Text.ToUpperFirst(), out BuildCommand buildCommand))
                    throw new ElementParseException("link", $"Build command \"{value.Text}\" doesn't exist.");

                return new BuildCommandLink(args, value, buildCommand);
            }
        }
    }
}
