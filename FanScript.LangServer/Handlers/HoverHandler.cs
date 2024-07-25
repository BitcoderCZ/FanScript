using FanScript.Compiler;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.LangServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers
{
    internal class HoverHandler : HoverHandlerBase
    {
        private readonly ILanguageServerFacade facade;

        private TextDocumentHandler? documentHandler;

        public HoverHandler(ILanguageServerFacade _facade)
        {
            facade = _facade;
        }

        public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            documentHandler ??= facade.Workspace.GetService(typeof(TextDocumentHandler)) as TextDocumentHandler;

            if (documentHandler is null)
                return null;

            await Task.Yield();

            Document document = documentHandler.GetDocument(request.TextDocument.Uri);
            SyntaxTree? tree = document.Tree;
            Compilation? compilation = document.Compilation;

            if (tree is null || compilation is null)
                return null;

            var node = tree.FindNode(request.Position.ToSpan(tree.Text));

            if (node is null)
                return null;

            switch (node.Parent)
            {
                case NameExpressionSyntax name when name.Parent is PropertyExpressionSyntax prop:
                    return getHoverForProperty(prop.IdentifierToken.Text, name.IdentifierToken.Text, name.Location);
                case NameExpressionSyntax name:
                    return getHoverByVariableName(name.IdentifierToken.Text, name.Location);
                case PropertyExpressionSyntax property:
                    return getHoverByVariableName(property.IdentifierToken.Text, property.IdentifierToken.Location);
                case AssignableVariableClauseSyntax variableClause:
                    return getHoverByVariableName(variableClause.IdentifierToken.Text, variableClause.Location);
                case AssignablePropertyClauseSyntax propertyClause:
                    {
                        if (node == propertyClause.VariableToken)
                            return getHoverByVariableName(propertyClause.VariableToken.Text, propertyClause.VariableToken.Location);
                        else if (node == propertyClause.IdentifierToken)
                            return getHoverForProperty(propertyClause.VariableToken.Text, propertyClause.IdentifierToken.Text, propertyClause.IdentifierToken.Location);
                    }
                    break;
                case CallExpressionSyntax call when node == call.Identifier:
                    {
                        List<FunctionSymbol> functions = compilation.GetFunctions()
                            .Where(func => func.Name == call.Identifier.Text && func.Parameters.Length == call.ArgumentClause.Arguments.Count)
                            .ToList();

                        if (functions.Count == 0)
                            break;

                        return new Hover()
                        {
                            Contents = new MarkedStringsOrMarkupContent(functions
                                .Select(func =>
                                    new MarkedString("#### " + func.ToString() +
                                        (string.IsNullOrEmpty(func.Description) ?
                                            string.Empty :
                                            "\n" + func.Description)
                                    )
                                )
                            ),
                            Range = call.Identifier.Location.ToRange(),
                        };
                    }
                case SpecialBlockStatementSyntax sb when node == sb.Identifier:
                    {
                        if (!Enum.TryParse(sb.Identifier.Text, out SpecialBlockType type))
                            break;

                        var info = type.GetInfo();

                        return new Hover()
                        {
                            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                            {
                                Kind = MarkupKind.Markdown,
                                Value = "#### " + info.ToString() +
                                    (string.IsNullOrEmpty(info.Description) ?
                                        string.Empty :
                                        "\n" + info.Description)
                            }),
                            Range = sb.Identifier.Location.ToRange(),
                        };
                    }
            }

            return null;

            // TODO/CUSTOM_FUNCTIONS: won't work
            Hover? getHoverByVariableName(string name, TextLocation location)
            {
                IEnumerable<VariableSymbol> variables = document.Compilation!.GetVariables();
                VariableSymbol? variable = variables.FirstOrDefault(var => var.Name == name);
                if (variable is null)
                    return null;
                else
                    return new Hover()
                    {
                        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                        {
                            Kind = MarkupKind.PlainText,
                            Value = variableInfo(variable),
                        }),
                        Range = location.ToRange(),
                    };
            }
            // TODO/CUSTOM_FUNCTIONS: won't work
            Hover? getHoverForProperty(string baseVarName, string propName, TextLocation location)
            {
                IEnumerable<VariableSymbol> variables = document.Compilation!.GetVariables();
                VariableSymbol? variable = variables.FirstOrDefault(var => var.Name == baseVarName);
                if (variable is null)
                    return null;

                PropertyDefinitionSymbol? property = variable.Type.GetProperty(propName);
                if (property is null)
                    return null;

                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.PlainText,
                        Value = propertyInfo(new PropertySymbol(property, variable)),
                    }),
                    Range = location.ToRange(),
                };
            }
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
            => new HoverRegistrationOptions()
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
            };

        private string variableInfo(VariableSymbol variable)
        {
            StringBuilder builder = new StringBuilder();
            variable.Modifiers.ToSyntaxString(builder);
            builder.Append(' ');
            builder.Append(variable.Type);
            builder.Append(' ');
            builder.Append(variable.Name);

            if (variable.Constant is not null)
            {
                builder.Append(" = ");
                builder.Append(variable.Constant.Value);
            }

            return builder.ToString();
        }

        private string propertyInfo(PropertySymbol property)
        {
            StringBuilder builder = new StringBuilder();
            property.Modifiers.ToSyntaxString(builder);
            builder.Append(' ');
            builder.Append(property.Type);
            builder.Append(' ');
            builder.Append(property.BaseVariable.Type);
            builder.Append('.');
            builder.Append(property.Name);

            if (property.Constant is not null)
            {
                builder.Append(" = ");
                builder.Append(property.Constant.Value);
            }

            return builder.ToString();
        }
    }
}
