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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers
{
    internal class CompletionHandler : CompletionHandlerBase
    {
        #region data
        private static readonly ImmutableArray<CompletionItem> keywords = Enum.GetValues<SyntaxKind>()
            .Where(kind => kind.IsKeyword())
            .Select(kind => kind.GetText()!)
            .Where(text => TypeSymbol.GetType(text) == TypeSymbol.Error) // remove types
            .Select(text => new CompletionItem()
            {
                Label = text,
                Kind = CompletionItemKind.Keyword,
            })
            .ToImmutableArray();
        private static readonly ImmutableArray<CompletionItem> modifiers = Enum.GetValues<SyntaxKind>()
            .Where(kind => kind.IsModifier())
            .Select(kind => new CompletionItem()
            {
                Label = kind.GetText()!,
                Kind = CompletionItemKind.Keyword,
            })
            .ToImmutableArray();
        private static readonly ImmutableArray<CompletionItem> types = TypeSymbol.BuiltInTypes
            .Select(type => new CompletionItem()
            {
                Label = type.Name,
                Kind = CompletionItemKind.Class, // use Struct instead?
            })
            .ToImmutableArray();
        private static readonly ImmutableArray<CompletionItem> specialBlockTypes = Enum.GetValues<SpecialBlockType>()
            .Select(sbt =>
            {
                var info = sbt.GetInfo();

                StringBuilder builder = new StringBuilder()
                    .Append(sbt.ToString());

                if (info.Parameters.Length != 0)
                {
                    builder.Append('(');
                    for (int i = 0; i < info.Parameters.Length; i++)
                    {
                        var param = info.Parameters[i];

                        if (i != 0)
                            builder.Append(", ");

                        if (param.Modifiers.HasFlag(Modifiers.Ref))
                            builder.Append("ref ");
                        if (param.Modifiers.HasFlag(Modifiers.Out))
                        {
                            builder.Append("out ");
                            builder.Append(param.Type);
                            builder.Append(' ');
                        }

                        if (param.IsConstant)
                            builder.Append(param.Name);
                    }

                    builder.Append(')');
                }

                string insertText = builder
                    .ToString();

                return new CompletionItem()
                {
                    Label = info.ToString(),
                    LabelDetails = new CompletionItemLabelDetails()
                    {
                        Description = info.Description,
                    },
                    Kind = CompletionItemKind.Function,
                    SortText = sbt.ToString(),
                    FilterText = sbt.ToString(),
                    InsertText = insertText,
                };
            })
            .ToImmutableArray();
        private static readonly ImmutableArray<CompletionItem> values =
            new List<string>() { "true", "false" }
            .Select(text => new CompletionItem()
            {
                Label = text,
                Kind = CompletionItemKind.Value,
            })
            .ToImmutableArray();
        #endregion

        private readonly ILanguageServerFacade facade;

        private TextDocumentHandler? documentHandler;

        public CompletionHandler(ILanguageServerFacade _facade)
        {
            facade = _facade;
        }

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            return new Task<CompletionItem>(() => request, cancellationToken);
        }

        [Flags]
        enum CurrentRecomendations : ushort
        {
            Keywords = 1 << 0,
            Modifiers = 1 << 1,
            Types = 1 << 2,
            SpecialBlockTypes = 1 << 3,
            Values = 1 << 4,
            Variables = 1 << 5,
            Functions = 1 << 6,
            NewIdentifier = 1 << 7,

            InExpression = Values | Variables | Functions,

            All = ushort.MaxValue,
        }
        public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            documentHandler ??= facade.Workspace.GetService(typeof(TextDocumentHandler)) as TextDocumentHandler;

            if (documentHandler is null)
                return new CompletionList();

            await Task.Yield();

            Document document = documentHandler.GetDocument(request.TextDocument.Uri);
            SyntaxTree? tree = document.Tree;
            Compilation? compilation = document.Compilation;
            var _ = compilation?.GlobalScope; // make BoundReult(s) available to getRecomendations

            CurrentRecomendations recomendations;

            List<CompletionItem>? recomendationsList = null;

            if (tree is null)
                recomendations = CurrentRecomendations.All;
            else
            {
                var node = tree.FindNode(request.Position.ToSpan(tree.Text) - 1);

                if (node is null)
                    recomendations = CurrentRecomendations.All;
                else
                    recomendations = getRecomendations(node, out recomendationsList);
            }

            if (recomendations == 0 && recomendationsList is null)
                return new CompletionList();

            int length = 0;
            if (recomendationsList is not null)
                length += recomendationsList.Count;

            if (recomendations.HasFlag(CurrentRecomendations.Keywords))
                length += keywords.Length;
            if (recomendations.HasFlag(CurrentRecomendations.Modifiers))
                length += modifiers.Length;
            if (recomendations.HasFlag(CurrentRecomendations.Types))
                length += types.Length;
            if (recomendations.HasFlag(CurrentRecomendations.SpecialBlockTypes))
                length += specialBlockTypes.Length;
            if (recomendations.HasFlag(CurrentRecomendations.Values))
                length += values.Length;

            VariableSymbol[]? variables = null;
            FunctionSymbol[]? functions = null;
            if (compilation is not null)
            {
                if (recomendations.HasFlag(CurrentRecomendations.Variables))
                    length += (variables = compilation.GetVariables().ToArray()).Length;
                if (recomendations.HasFlag(CurrentRecomendations.Functions))
                    length += (functions = compilation.GetFunctions().ToArray()).Length;
            }

            List<CompletionItem> result = new List<CompletionItem>(length);

            if (recomendationsList is not null)
                result.AddRange(recomendationsList);

            if (recomendations.HasFlag(CurrentRecomendations.Keywords))
                result.AddRange(keywords);
            if (recomendations.HasFlag(CurrentRecomendations.Modifiers))
                result.AddRange(modifiers);
            if (recomendations.HasFlag(CurrentRecomendations.Types))
                result.AddRange(types);
            if (recomendations.HasFlag(CurrentRecomendations.SpecialBlockTypes))
                result.AddRange(specialBlockTypes);
            if (recomendations.HasFlag(CurrentRecomendations.Values))
                result.AddRange(values);

            if (variables is not null)
                result.AddRange(variables
                    .Select(var => new CompletionItem()
                    {
                        Label = var.ToString(),
                        LabelDetails = new CompletionItemLabelDetails()
                        {
                            Description = (var.Constant is null ?
                                    string.Empty :
                                    var.Constant.Value + " ")
                                + (var.Modifiers == 0 ?
                                    string.Empty :
                                    var.Modifiers.ToString()),
                        },
                        Kind = var.Modifiers.HasFlag(Modifiers.Constant) ?
                            CompletionItemKind.Constant :
                            CompletionItemKind.Variable,
                        SortText = var.Name,
                        FilterText = var.Name,
                        InsertText = var.Name,
                    })
                );
            if (functions is not null)
                result.AddRange(functions
                    .Select(fun => new CompletionItem()
                    {
                        Label = fun.Type + " " + fun.Name,
                        LabelDetails = new CompletionItemLabelDetails()
                        {
                            Detail = fun.ToString(onlyParams: true),
                            Description = fun.Description
                        },
                        Kind = CompletionItemKind.Function,
                        SortText = fun.Name,
                        FilterText = fun.Name,
                        InsertText = getInsertText(fun),
                    })
                );

            return new CompletionList(result);
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
            => new CompletionRegistrationOptions()
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("fanscript"),
                CompletionItem = new CompletionRegistrationCompletionItemOptions()
                {
                    LabelDetailsSupport = true,
                }
            };

        private string getInsertText(FunctionSymbol function)
        {
            StringBuilder builder = new StringBuilder()
                .Append(function.Name)
                .Append('(');

            for (int i = 0; i < function.Parameters.Length; i++)
            {
                ParameterSymbol param = function.Parameters[i];

                if (i != 0)
                    builder.Append(", ");

                if (param.Modifiers.HasFlag(Modifiers.Ref))
                    builder.Append("ref ");
                else if (param.Modifiers.HasFlag(Modifiers.Out))
                {
                    builder.Append("out ");
                    builder.Append(param.Type);
                    builder.Append(' ');
                }
            }

            return builder
                .Append(')')
                .ToString();
        }

        private CurrentRecomendations getRecomendations(SyntaxNode node, out List<CompletionItem>? recomendationsList)
        {
            recomendationsList = null;

            if (node is not SyntaxToken token)
            {
                SyntaxNode? missing = fistMissing(node);
                if (missing is not null)
                    node = missing;
                else
                    return CurrentRecomendations.All;
            }

            SyntaxNode? parent = node.Parent;

            if (parent is null)
                return CurrentRecomendations.All;

            CurrentRecomendations? recomendation = getRecomendationsWithParent(node, parent, out recomendationsList);

            return recomendation ?? CurrentRecomendations.All;
        }

        private CurrentRecomendations? getRecomendationsWithParent(SyntaxNode node, SyntaxNode parent, out List<CompletionItem>? recomendationsList)
        {
            recomendationsList = null;

            switch (parent)
            {
                case NameExpressionSyntax:
                    {
                        if (parent.Parent is not null)
                            return getRecomendationsWithParent(parent, parent.Parent, out recomendationsList);
                    }
                    break;
                // TODO:
                //case PropertyExpressionSyntax property:
                //    {
                //        if (node == property.IdentifierToken && property.BoundResult is BoundVariableExpression varEx && varEx.Variable is PropertySymbol propSymbol)
                //        {
                //            TypeSymbol baseType = propSymbol.Expression.Type;

                //            recomendationsList = baseType.Properties
                //                .Select(item =>
                //                {
                //                    var (name, definition) = item;

                //                    return new CompletionItem()
                //                    {
                //                        Label = definition.Type + " " + baseType + "." + name,
                //                        Kind = CompletionItemKind.Property,
                //                        SortText = name,
                //                        FilterText = name,
                //                        InsertText = name,
                //                    };
                //                })
                //                .ToList();
                //            return 0;
                //        }
                //    }
                //    break;
                case AssignableVariableClauseSyntax:
                    return CurrentRecomendations.NewIdentifier;
                case CallExpressionSyntax call:
                    {
                        if (node == call.Identifier)
                            return CurrentRecomendations.Functions;
                    }
                    break;
                case ArgumentClauseSyntax argumentClause:
                    return CurrentRecomendations.InExpression | CurrentRecomendations.Modifiers | CurrentRecomendations.Types;
                case SpecialBlockStatementSyntax specialBlock:
                    {
                        if (node == specialBlock.Identifier)
                            return CurrentRecomendations.SpecialBlockTypes;
                    }
                    break;
                case AssignmentStatementSyntax assignmentStatement when node == assignmentStatement.Expression:
                case IfStatementSyntax ifStatement when node == ifStatement.Condition:
                case WhileStatementSyntax whileStatement when node == whileStatement.Condition:
                case ParenthesizedExpressionSyntax:
                case BinaryExpressionSyntax:
                case UnaryExpressionSyntax:
                case ArraySegmentExpressionSyntax:
                    return CurrentRecomendations.InExpression;
            }

            return null;
        }

        private SyntaxNode? fistMissing(SyntaxNode node)
        {
            foreach (SyntaxNode child in node.GetChildren())
            {
                if (child is not SyntaxToken token)
                {
                    SyntaxNode? res = fistMissing(child);
                    if (res is not null)
                        return res;
                }
                else if (token.IsMissing)
                    return token;
            }

            return null;
        }
    }
}
