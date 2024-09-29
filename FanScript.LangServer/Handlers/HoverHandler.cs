using FanScript.Compiler;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.LangServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.IO;
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

            var requestSpan = request.Position.ToSpan(tree.Text);
            var syntax = tree.FindSyntax(requestSpan);

            if (syntax is not SyntaxNode node)
                return null;

            ScopeWSpan? scope = null;
            foreach (var (func, funcScope) in compilation
                .GetScopes()
                .OrderBy(item => item.Value.Span.Length))
            {
                if (funcScope.Span.OverlapsWith(requestSpan))
                {
                    scope = funcScope.GetScopeAt(requestSpan.Start);
                    break;
                }
            }

            switch (node.Parent)
            {
                case NameExpressionSyntax name:
                    {
                        if (scope is null)
                            break;

                        if (name.Parent is PropertyExpressionSyntax propEx && name == propEx.Expression)
                        {
                            PropertySymbol? prop = resolveProperty(propEx, scope
                                .GetAllVariables()
                                .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                                .ToArray(), out _) as PropertySymbol;

                            if (prop is not null)
                                return getHoverForProperty(prop, node.Location);
                        }
                        else
                        {
                            VariableSymbol? varSymbol = scope
                                .GetAllVariables()
                                .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                                .FirstOrDefault(var => var.Name == name.IdentifierToken.Text);

                            if (varSymbol is not null)
                                return getHoverForVariable(varSymbol, node.Location);
                        }
                    }
                    break;
                case VariableDeclarationStatementSyntax variableDeclarationStatement when node == variableDeclarationStatement.IdentifierToken:
                case AssignableVariableClauseSyntax variableClause when node == variableClause.IdentifierToken:
                case AssignablePropertyClauseSyntax propertyClause when node == propertyClause.VariableToken:
                    {
                        if (scope is null || node is not SyntaxToken token)
                            break;

                        VariableSymbol? varSymbol = scope
                            .GetAllVariables()
                            .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                            .FirstOrDefault(var => var.Name == token.Text);

                        if (varSymbol is not null)
                            return getHoverForVariable(varSymbol, node.Location);
                    }
                    break;
                case AssignablePropertyClauseSyntax propertyClause when node == propertyClause.IdentifierToken:
                    {
                        if (scope is null)
                            break;

                        PropertySymbol? prop = resolveProperty(new PropertyExpressionSyntax(tree, new NameExpressionSyntax(tree, propertyClause.VariableToken), propertyClause.DotToken, new NameExpressionSyntax(tree, propertyClause.IdentifierToken)), scope
                              .GetAllVariables()
                              .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                              .ToArray(), out _) as PropertySymbol;

                        if (prop is not null)
                            return getHoverForProperty(prop, node.Location);
                    }
                    break;
                case CallExpressionSyntax call when node == call.Identifier:
                    {
                        if (call.Parent is PropertyExpressionSyntax propEx && call == propEx.Expression)
                        {
                            IEnumerable<FunctionSymbol>? funcs = null;
                            TypeSymbol? baseType = null;
                            if (scope is not null)
                                funcs = resolveProperty(propEx, scope
                                .GetAllVariables()
                                .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                                .ToArray(), out baseType) as IEnumerable<FunctionSymbol>;

                            if (funcs is not null && baseType is not null)
                                return getHoverForMethods(funcs.ToArray(), baseType, node.Location);
                        }
                        else
                        {
                            var functions = compilation
                                .GetFunctions()
                                .Where(func => func.Name == call.Identifier.Text);

                            return getHoverForFunctions(functions.ToArray(), node.Location);
                        }
                        //BoundCallExpression? boundCall;
                        //if (call.BoundResult is null || (boundCall = call.BoundResult as BoundCallExpression) is null)
                        //    break;

                        //return new Hover()
                        //{
                        //    Contents = new MarkedStringsOrMarkupContent(
                        //        new MarkedString("#### " + boundCall.Function.ToString() +
                        //            (string.IsNullOrEmpty(boundCall.Function.Description) ?
                        //                string.Empty :
                        //                "\n" + boundCall.Function.Description)
                        //        )
                        //    ),
                        //    Range = node.Location.ToRange(),
                        //};
                    }
                    break;
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

            object? resolveProperty(PropertyExpressionSyntax syntax, VariableSymbol[] variables, out TypeSymbol? baseType)
            {
                VariableSymbol? baseVar = null;
                baseType = null;

                switch (syntax.BaseExpression)
                {
                    case PropertyExpressionSyntax propEx:
                        baseVar = resolveProperty(propEx, variables, out _) as VariableSymbol;
                        break;
                    case NameExpressionSyntax nameEx:
                        baseVar = variables.FirstOrDefault(var => var.Name == nameEx.IdentifierToken.Text);
                        break;
                    default:
                        return null;
                }

                if (baseVar is null)
                    return null;

                baseType = baseVar.Type;

                if (syntax.Expression is NameExpressionSyntax name)
                {
                    PropertyDefinitionSymbol? propDef = baseVar.Type.GetProperty(name.IdentifierToken.Text);
                    return propDef is null ?
                        null :
                        new PropertySymbol(propDef, new BoundVariableExpression(null!, baseVar));
                }
                else if (syntax.Expression is CallExpressionSyntax call)
                {
                    // method (instance function)
                    return compilation
                        .GetFunctions()
                        .Where(func => func.IsMethod && func.Name == call.Identifier.Text)
                        .OrderBy(func => Math.Abs(func.Parameters.Length - call.Arguments.Count));
                }

                return null;
            }

            Hover getHoverForVariable(VariableSymbol variable, TextLocation location)
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
            Hover getHoverForProperty(PropertySymbol property, TextLocation location)
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
            Hover? getHoverForFunctions(FunctionSymbol[] functions, TextLocation location)
            {
                if (functions.Length == 0)
                    return null;

                MarkedString[] results = new MarkedString[functions.Length];

                for (int i = 0; i < functions.Length; i++)
                    results[i] = new MarkedString(functionInfo(functions[i]));

                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(results),
                    Range = location.ToRange(),
                };
            }
            Hover? getHoverForMethods(FunctionSymbol[] methods, TypeSymbol baseType, TextLocation location)
            {
                if (methods.Length == 0)
                    return null;

                MarkedString[] results = new MarkedString[methods.Length];

                for (int i = 0; i < methods.Length; i++)
                    results[i] = new MarkedString(methodInfo(baseType, methods[i]));

                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(results),
                    Range = location.ToRange(),
                };
            }
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
            => new HoverRegistrationOptions()
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
            };

        private static string variableInfo(VariableSymbol variable)
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
        private static string propertyInfo(PropertySymbol property)
        {
            StringBuilder builder = new StringBuilder();

            TypeSymbol baseType = property.Expression.Type;
            if (baseType.IsGenericInstance)
                baseType = TypeSymbol.GetGenericDefinition(baseType);

            property.Modifiers.ToSyntaxString(builder);
            builder.Append(' ');
            builder.Append(property.Type);
            builder.Append(' ');
            builder.Append(baseType);
            builder.Append('.');
            builder.Append(property.Name);

            if (property.Constant is not null)
            {
                builder.Append(" = ");
                builder.Append(property.Constant.Value);
            }

            return builder.ToString();
        }
        private static string functionInfo(FunctionSymbol function)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(function.ToString());

            if (!string.IsNullOrEmpty(function.Description))
            {
                builder.Append(" - ");
                builder.Append(function.Description);
            }

            return builder.ToString();
        }
        private static string methodInfo(TypeSymbol baseType, FunctionSymbol method)
        {
            StringBuilder builder = new StringBuilder();
            using StringWriter writer = new StringWriter(builder);

            if (baseType.IsGenericInstance)
                baseType = TypeSymbol.GetGenericDefinition(baseType);

            method.Modifiers.ToSyntaxString(builder);
            builder.Append(' ');
            builder.Append(method.Type);
            builder.Append(' ');
            builder.Append(baseType);
            builder.Append('.');
            builder.Append(method.Name);

            SymbolPrinter.WriteFunctionTo(method, writer, true, true);

            if (!string.IsNullOrEmpty(method.Description))
            {
                builder.Append(" - ");
                builder.Append(method.Description);
            }

            return builder.ToString();
        }
    }
}
