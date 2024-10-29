using FanScript.Compiler;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.Documentation.Attributes;
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

            if (node is SyntaxToken sToken)
            {
                if (sToken.Kind.IsModifier())
                    return getHoverForModifier(ModifiersE.FromKind(sToken.Kind), sToken.Location);

                TypeSymbol? type;
                if (sToken.Text == TypeSymbol.Null.Name)
                    type = TypeSymbol.Null;
                else
                    type = TypeSymbol.GetType(sToken.Text);

                if (type != TypeSymbol.Error)
                    return getHoverForType(type, sToken.Location);
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
                            if (node is not SyntaxToken token)
                                break;

                            VariableSymbol? varSymbol = scope
                                .GetAllVariables()
                                .Concat(compilation.GetVariables().Where(var => var.IsGlobal))
                                .FirstOrDefault(var => var.Name == token.Text);

                            if (varSymbol is not null)
                                return getHoverForVariable(varSymbol, node.Location);
                        }
                    }
                    break;
                case VariableDeclarationStatementSyntax variableDeclarationStatement when node == variableDeclarationStatement.IdentifierToken:
                case AssignmentStatementSyntax assignment when node == assignment.Destination:
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
                    }
                    break;
                case CallStatementSyntax call when node == call.Identifier:
                    {
                        var functions = compilation
                            .GetFunctions()
                            .Where(func => func.Name == call.Identifier.Text);

                        return getHoverForFunctions(functions.ToArray(), node.Location);
                    }
                case EventStatementSyntax sb when node == sb.Identifier:
                    {
                        if (!Enum.TryParse(sb.Identifier.Text, out EventType type))
                            break;

                        var info = type.GetInfo();
                        var doc = DocUtils.GetAttribute<EventType, EventDocAttribute>(type);

                        string val = info.ToString();

                        if (!string.IsNullOrEmpty(doc.Info))
                            val += " - " + doc.Info;

                        return new Hover()
                        {
                            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                            {
                                Kind = MarkupKind.Markdown,
                                Value = val
                            }),
                            Range = node.Location.ToRange(),
                        };
                    }
                case PostfixExpressionSyntax pe when node == pe.IdentifierToken:
                case PostfixStatementSyntax ps when node == ps.IdentifierToken:
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
                case BuildCommandStatementSyntax bc when node == bc.Identifier:
                    {
                        BuildCommand? command = BuildCommandE.Parse(bc.Identifier.Text);

                        if (command is null)
                            break;

                        var doc = DocUtils.GetAttribute<BuildCommand, BuildCommandDocAttribute>(command.Value);

                        return new Hover()
                        {
                            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                            {
                                Kind = MarkupKind.PlainText,
                                Value = DocUtils.ParseAndBuild(doc.Info, null),
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

            Hover? getHoverForVariable(VariableSymbol variable, TextLocation location)
            {
                if (variable.Modifiers.HasFlag(Modifiers.Constant) && variable.Modifiers.HasFlag(Modifiers.Global))
                {
                    foreach (var group in Constants.Groups)
                    {
                        if (variable.Name.StartsWith(group.Name, StringComparison.Ordinal))
                        {
                            for (int i = 0; i < group.Values.Length; i++)
                            {
                                if (variable.Name == group.Name + "_" + group.Values[i].Name)
                                {
                                    var doc = Constants.ConstantToDoc[group];

                                    string info = doc.Info is null ? string.Empty : DocUtils.ParseAndBuild(doc.Info, null);

                                    if (doc.ValueInfos is not null && !string.IsNullOrEmpty(doc.ValueInfos[i]))
                                        info += "\n" + DocUtils.ParseAndBuild(doc.ValueInfos[i], null);

                                    if (info.Length == 0)
                                        return null;

                                    return new Hover()
                                    {
                                        Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                                        {
                                            Kind = MarkupKind.PlainText,
                                            Value = info,
                                        }),
                                        Range = location.ToRange(),
                                    };
                                }
                            }
                        }
                    }
                }

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
            Hover getHoverForModifier(Modifiers mod, TextLocation location)
            {
                ModifierDocAttribute doc = DocUtils.GetAttribute<Modifiers, ModifierDocAttribute>(mod);

                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.PlainText,
                        Value = DocUtils.ParseAndBuild(doc.Info, null),
                    }),
                    Range = location.ToRange(),
                };
            }
            Hover getHoverForType(TypeSymbol type, TextLocation location)
            {
                TypeDocAttribute doc = TypeSymbol.TypeToDoc[type];

                return new Hover()
                {
                    Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.PlainText,
                        Value = DocUtils.ParseAndBuild(doc.Info, null),
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
            FunctionDocAttribute? doc;
            if (function is BuiltinFunctionSymbol)
                doc = BuiltinFunctions.FunctionToDoc[function];
            else
                doc = new FunctionDocAttribute();

            StringBuilder builder = new StringBuilder();

            builder.Append(function.ToString());

            if (!string.IsNullOrEmpty(doc.Info))
            {
                builder.Append(" - ");
                builder.Append(DocUtils.ParseAndBuild(doc.Info, function));
            }

            return builder.ToString();
        }
        private static string methodInfo(TypeSymbol baseType, FunctionSymbol method)
        {
            FunctionDocAttribute? doc;
            if (method is BuiltinFunctionSymbol)
                doc = BuiltinFunctions.FunctionToDoc[method];
            else
                doc = new FunctionDocAttribute();

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

            if (!string.IsNullOrEmpty(doc.Info))
            {
                builder.Append(" - ");
                builder.Append(DocUtils.ParseAndBuild(doc.Info, method));
            }

            return builder.ToString();
        }
    }
}
