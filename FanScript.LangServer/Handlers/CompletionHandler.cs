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
        private static readonly ImmutableArray<CompletionItem> eventTypes = Enum.GetValues<EventType>()
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
        private static readonly ImmutableArray<CompletionItem> buildCommands =
            Enum.GetNames<BuildCommand>()
            .Select(name => new CompletionItem()
            {
                Label = name.ToLowerInvariant(),
                Kind = CompletionItemKind.Keyword, // TODO: figure out what to use for this
            })
            .ToImmutableArray();
        #endregion

        private readonly ILanguageServerFacade facade;

        private TextDocumentHandler? documentHandler;

        public CompletionHandler(ILanguageServerFacade facade)
        {
            this.facade = facade;
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
            Events = 1 << 3,
            Values = 1 << 4,
            Variables = 1 << 5,
            Functions = 1 << 6,
            Methods = 1 << 7,
            NewIdentifier = 1 << 8,
            BuildCommand = 1 << 9,

            InExpression = Values | Variables | Functions | Methods,

            UnknownSituation = Keywords | Modifiers | Types | Values | Variables | Functions,
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

            CurrentRecomendations recomendations;

            List<CompletionItem>? recomendationsList = null;
            bool inProp = false;

            TextSpan? requestSpan = null;

            if (tree is null)
                recomendations = CurrentRecomendations.All;
            else
            {
                requestSpan = request.Position.ToSpan(tree.Text) - 1;
                var syntax = tree.FindSyntax(requestSpan.Value);

                if (syntax is SyntaxNode node)
                    recomendations = getRecomendations(node, out recomendationsList, out inProp);
                else if (syntax is SyntaxTrivia trivia)
                {
                    switch (trivia.Kind)
                    {
                        case SyntaxKind.SingleLineCommentTrivia:
                        case SyntaxKind.MultiLineCommentTrivia:
                            recomendations = 0;
                            break;
                        default:
                            recomendations = CurrentRecomendations.UnknownSituation;
                            break;
                    }
                }
                else
                    recomendations = CurrentRecomendations.UnknownSituation;
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
            if (recomendations.HasFlag(CurrentRecomendations.Events))
                length += eventTypes.Length;
            if (recomendations.HasFlag(CurrentRecomendations.Values))
                length += values.Length;
            if (recomendations.HasFlag(CurrentRecomendations.BuildCommand))
                length += buildCommands.Length;

            VariableSymbol[]? variables = null;
            FunctionSymbol[]? functions = null;
            FunctionSymbol[]? methods = null;
            ScopeWSpan? scope = null;
            if (compilation is not null)
            {
                if (requestSpan is not null)
                {
                    foreach (var (func, funcScope) in compilation
                        .GetScopes()
                        .OrderBy(item => item.Value.Span.Length))
                    {
                        if (funcScope.Span.OverlapsWith(requestSpan.Value))
                        {
                            scope = funcScope.GetScopeAt(requestSpan.Value.Start);
                            break;
                        }
                    }
                }

                if (recomendations.HasFlag(CurrentRecomendations.Variables))
                {
                    if (scope is null)
                        length += (variables = compilation.GetVariables().ToArray()).Length;
                    else
                    {
                        TextLocation loc = new TextLocation(document.Tree!.Text, scope.Span);

                        variables = scope
                            .GetAllVariables()
                            .Concat(compilation.GetVariables().Where(var => var is GlobalVariableSymbol))
                            .ToArray();
                    }
                }
                if (recomendations.HasFlag(CurrentRecomendations.Functions))
                    length += (functions = compilation.GetFunctions().Where(func => !func.IsMethod).ToArray()).Length;
                if (recomendations.HasFlag(CurrentRecomendations.Methods))
                    length += (methods = compilation.GetFunctions().Where(func => func.IsMethod).ToArray()).Length;
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
            if (recomendations.HasFlag(CurrentRecomendations.Events))
                result.AddRange(eventTypes);
            if (recomendations.HasFlag(CurrentRecomendations.Values))
                result.AddRange(values);
            if (recomendations.HasFlag(CurrentRecomendations.BuildCommand))
                result.AddRange(buildCommands);

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
                        InsertText = getInsertText(fun, false),
                    })
                );
            if (methods is not null)
                result.AddRange(methods
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
                        InsertText = getInsertText(fun, inProp),
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
                },
                TriggerCharacters = new Container<string>(".", "#"),
            };

        private CurrentRecomendations getRecomendations(SyntaxNode node, out List<CompletionItem>? recomendationsList, out bool inProp)
        {
            recomendationsList = null;
            inProp = false;

            if (node is not SyntaxToken token)
            {
                SyntaxNode? missing = fistMissing(node);
                if (missing is not null)
                    node = missing;
                else
                    return CurrentRecomendations.UnknownSituation;
            }

            SyntaxNode? parent = node.Parent;

            if (parent is null)
                return CurrentRecomendations.UnknownSituation;

            CurrentRecomendations? recomendation = getRecomendationsWithParent(node, parent, out recomendationsList, out inProp);

            return recomendation ?? CurrentRecomendations.UnknownSituation;
        }

        private CurrentRecomendations? getRecomendationsWithParent(SyntaxNode node, SyntaxNode parent, out List<CompletionItem>? recomendationsList, out bool inProp)
        {
            recomendationsList = null;
            inProp = false;

            switch (parent)
            {
                case LiteralExpressionSyntax:
                    return 0;
                case NameExpressionSyntax:
                    {
                        if (parent.Parent is not null)
                            return getRecomendationsWithParent(parent, parent.Parent, out recomendationsList, out inProp);
                    }
                    break;
                case PropertyExpressionSyntax property:
                    {
                        inProp = true;

                        if (node == property.DotToken)
                        {
                            recomendationsList = TypeSymbol.AllTypes
                                .Aggregate(new List<CompletionItem>(), (list, type) =>
                                {
                                    list.EnsureCapacity(list.Count + type.Properties.Count);
                                    foreach (var (_, definition) in type.Properties)
                                        list.Add(completionFor(definition, type));

                                    return list;
                                });

                            return CurrentRecomendations.Methods;
                        }
                        //else if ((parent.BoundResult as BoundVariableExpression)?.Variable is PropertySymbol prop)
                        //{
                        //    if (node == property.Expression)
                        //    {
                        //        recomendationsList = prop.Expression.Type.Properties
                        //        .Select(item =>
                        //        {
                        //            var (_, definition) = item;

                        //            return completionFor(definition, prop.Expression.Type);
                        //        })
                        //                .ToList();
                        //        return 0;
                        //    }
                        //}
                    }
                    break;
                case AssignableVariableClauseSyntax:
                    return CurrentRecomendations.NewIdentifier;
                case CallExpressionSyntax call:
                    {
                        if (node == call.Identifier)
                            return CurrentRecomendations.Functions | CurrentRecomendations.Methods;
                    }
                    break;
                case ArgumentClauseSyntax argumentClause:
                    return CurrentRecomendations.InExpression | CurrentRecomendations.Modifiers | CurrentRecomendations.Types;
                case EventStatementSyntax @event:
                    {
                        if (node == @event.Identifier)
                            return CurrentRecomendations.Events;
                    }
                    break;
                case BuildCommandStatementSyntax buildCommand:
                    return CurrentRecomendations.BuildCommand;
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

        private string getInsertText(FunctionSymbol function, bool inProp)
        {
            StringBuilder builder = new StringBuilder()
                .Append(function.Name)
                .Append('(');

            int start = inProp ? 1 : 0;
            for (int i = start; i < function.Parameters.Length; i++)
            {
                ParameterSymbol param = function.Parameters[i];

                if (i != start)
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

        private CompletionItem completionFor(PropertyDefinitionSymbol definition, TypeSymbol baseType)
            => new CompletionItem()
            {
                Label = definition.Type + " " + baseType + "." + definition.Name,
                Kind = CompletionItemKind.Property,
                SortText = definition.Name,
                FilterText = definition.Name,
                InsertText = definition.Name,
            };

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
