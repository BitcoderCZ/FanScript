using FanScript.Compiler;
using FanScript.Compiler.Binding;
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
                case NameExpressionSyntax name:
                    {
                        if (name.BoundResult is BoundVariableExpression varEx)
                            return getHoverForVariable(varEx.Variable, name.Location);
                        else
                            return getHoverByVariableName(name.IdentifierToken.Text, name.Location);
                    }
                    // TODO:
                //case PropertyExpressionSyntax property:
                //    {
                //        if (node == property.IdentifierToken && property.BoundResult is BoundVariableExpression varEx && varEx.Variable is PropertySymbol prop)
                //            return getHoverForProperty(prop, node.Location);
                //    }
                //    break;
                case VariableDeclarationStatementSyntax variableDeclarationStatement:
                    {
                        if (node == variableDeclarationStatement.IdentifierToken && variableDeclarationStatement.BoundResult is BoundVariableDeclarationStatement boundDeclaration)
                            return getHoverForVariable(boundDeclaration.Variable, node.Location);
                    }
                    break;
                case AssignableVariableClauseSyntax variableClause:
                    {
                        if (variableClause.BoundResult is VariableSymbol varSymbol)
                            return getHoverForVariable(varSymbol, variableClause.Location);
                        else
                            return getHoverByVariableName(variableClause.IdentifierToken.Text, variableClause.Location);
                    }
                case AssignablePropertyClauseSyntax propertyClause:
                    {
                        if (node == propertyClause.VariableToken)
                        {
                            if (propertyClause.BoundResult is PropertySymbol propSymbol && propSymbol.Expression is BoundVariableExpression varEx)
                                return getHoverForVariable(varEx.Variable, varEx.Syntax.Location);
                            else
                                return getHoverByVariableName(propertyClause.VariableToken.Text, propertyClause.VariableToken.Location);
                        }
                        else if (node == propertyClause.IdentifierToken && propertyClause.BoundResult is PropertySymbol propSymbol)
                            return getHoverForProperty(propSymbol, propertyClause.IdentifierToken.Location);
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

            Hover? getHoverByVariableName(string name, TextLocation location)
            {
                IEnumerable<VariableSymbol> variables = compilation.GetVariables();
                VariableSymbol? variable = variables.FirstOrDefault(var => var.Name == name);
                if (variable is null)
                    return null;
                else
                    return getHoverForVariable(variable, location);
            }
            Hover? getHoverForVariable(VariableSymbol variable, TextLocation location)
            {
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
            Hover? getHoverForProperty(PropertySymbol property, TextLocation location)
            {
                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.PlainText,
                        Value = propertyInfo(property),
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
            builder.Append(property.Expression.Type);
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
