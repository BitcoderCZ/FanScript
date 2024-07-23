using FanScript.Compiler;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
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

            if (document.Tree is null || document.Compilation is null)
                return null;

            var node = document.Tree.FindNode(request.Position.ToSpan(document.Tree.Text));

            if (node is null)
                return null;

            switch (node.Parent)
            {
                case NameExpressionSyntax name:
                    {
                        IEnumerable<VariableSymbol> variables = document.Compilation.GetVariables();
                        VariableSymbol? variable = variables.FirstOrDefault(var => var.Name == name.IdentifierToken.Text);

                        if (variable is not null)
                            return new Hover()
                            {
                                Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                                {
                                    Kind = MarkupKind.PlainText,
                                    Value = variableInfo(variable),
                                }),
                                Range = name.Location.ToRange(),
                            };
                    }
                    break;
                case CallExpressionSyntax call when node == call.Identifier:
                    {
                        List<FunctionSymbol> functions = document.Compilation.GetFunctions()
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
    }
}
