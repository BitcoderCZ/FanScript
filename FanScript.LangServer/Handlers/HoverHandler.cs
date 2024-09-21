using FanScript.Compiler;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.LangServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers
{
    internal class HoverHandler : HoverHandlerBase
    {
        private readonly ILanguageServerFacade facade;

        private TextDocumentHandler? documentHandler;

        public HoverHandler(ILanguageServerFacade facade)
        {
            this.facade = facade;
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

            var syntax = tree.FindSyntax(request.Position.ToSpan(tree.Text));

            if (syntax is not SyntaxNode node)
                return null;

            switch (node.Parent)
            {
                case NameExpressionSyntax name:
                    {
                        //if (name.BoundResult is BoundVariableExpression varEx)
                        //    return getHoverForVariable(varEx.Variable, name.Location);
                        //else if (name.Parent is PropertyExpressionSyntax propEx &&
                        //    (propEx.BoundResult as BoundVariableExpression)?.Variable is PropertySymbol prop)
                        //{
                        //    if (name == propEx.Expression)
                        //        return getHoverForProperty(prop, node.Location);
                        //}
                    }
                    break;
                case VariableDeclarationStatementSyntax variableDeclarationStatement:
                    {
                        //if (node == variableDeclarationStatement.IdentifierToken && variableDeclarationStatement.BoundResult is BoundVariableDeclarationStatement boundDeclaration)
                        //    return getHoverForVariable(boundDeclaration.Variable, node.Location);
                    }
                    break;
                case AssignableVariableClauseSyntax variableClause:
                    {
                        //if (variableClause.BoundResult is VariableSymbol varSymbol)
                        //    return getHoverForVariable(varSymbol, node.Location);
                    }
                    break;
                case AssignablePropertyClauseSyntax propertyClause:
                    {
                        //if (node == propertyClause.VariableToken)
                        //{
                        //    if (propertyClause.BoundResult is PropertySymbol propSymbol && propSymbol.Expression is BoundVariableExpression varEx)
                        //        return getHoverForVariable(varEx.Variable, node.Location);
                        //}
                        //else if (node == propertyClause.IdentifierToken && propertyClause.BoundResult is PropertySymbol propSymbol)
                        //    return getHoverForProperty(propSymbol, node.Location);
                    }
                    break;
                //case CallExpressionSyntax call when node == call.Identifier:
                //    {
                //        BoundCallExpression? boundCall;
                //        if (call.BoundResult is null || (boundCall = call.BoundResult as BoundCallExpression) is null)
                //            break;

                //        return new Hover()
                //        {
                //            Contents = new MarkedStringsOrMarkupContent(
                //                new MarkedString("#### " + boundCall.Function.ToString() +
                //                    (string.IsNullOrEmpty(boundCall.Function.Description) ?
                //                        string.Empty :
                //                        "\n" + boundCall.Function.Description)
                //                )
                //            ),
                //            Range = node.Location.ToRange(),
                //        };
                //    }
                case EventStatementSyntax sb when node == sb.Identifier:
                    {
                        if (!Enum.TryParse(sb.Identifier.Text, out EventType type))
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
                            Range = node.Location.ToRange(),
                        };
                    }
            }

            return null;

            //Hover? getHoverForVariable(VariableSymbol variable, TextLocation location)
            //{
            //    return new Hover()
            //    {
            //        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            //        {
            //            Kind = MarkupKind.PlainText,
            //            Value = variableInfo(variable),
            //        }),
            //        Range = location.ToRange(),
            //    };
            //}
            //Hover? getHoverForProperty(PropertySymbol property, TextLocation location)
            //{
            //    return new Hover()
            //    {
            //        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            //        {
            //            Kind = MarkupKind.PlainText,
            //            Value = propertyInfo(property),
            //        }),
            //        Range = location.ToRange(),
            //    };
            //}
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
