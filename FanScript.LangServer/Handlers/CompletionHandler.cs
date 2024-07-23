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
                .Append(sbt.ToString())
                .Append('(');

                for (int i = 0; i < info.Parameters.Length; i++)
                {
                    var param = info.Parameters[i];

                    if (i != 0)
                        builder.Append(", ");

                    // TODO: support out once added
                    if (param.Modifiers.HasFlag(Modifiers.Ref))
                        builder.Append("ref ");

                    if (param.IsConstant)
                        builder.Append(param.Name);
                }

                string insertText = builder
                    .Append(')')
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

            if (document.Tree is null)
                return new CompletionList();

            var node = document.Tree.FindNode(request.Position.ToSpan(document.Tree.Text) - 1);

            if (node is null)
                return new CompletionList();

            CurrentRecomendations recomendation = getRecomendation(node);

            if (recomendation == 0)
                return new CompletionList();

            int length = 0;
            if (recomendation.HasFlag(CurrentRecomendations.Keywords))
                length += keywords.Length;
            if (recomendation.HasFlag(CurrentRecomendations.Modifiers))
                length += modifiers.Length;
            if (recomendation.HasFlag(CurrentRecomendations.Types))
                length += types.Length;
            if (recomendation.HasFlag(CurrentRecomendations.SpecialBlockTypes))
                length += specialBlockTypes.Length;
            if (recomendation.HasFlag(CurrentRecomendations.Values))
                length += values.Length;

            VariableSymbol[]? variables = null;
            FunctionSymbol[]? functions = null;
            if (document.Compilation is not null)
            {
                if (recomendation.HasFlag(CurrentRecomendations.Variables))
                    length += (variables = document.Compilation.GetVariables().ToArray()).Length;
                if (recomendation.HasFlag(CurrentRecomendations.Functions))
                    length += (functions = document.Compilation.GetFunctions().ToArray()).Length;
            }

            List<CompletionItem> result = new List<CompletionItem>(length);

            if (recomendation.HasFlag(CurrentRecomendations.Keywords))
                result.AddRange(keywords);
            if (recomendation.HasFlag(CurrentRecomendations.Modifiers))
                result.AddRange(modifiers);
            if (recomendation.HasFlag(CurrentRecomendations.Types))
                result.AddRange(types);
            if (recomendation.HasFlag(CurrentRecomendations.SpecialBlockTypes))
                result.AddRange(specialBlockTypes);
            if (recomendation.HasFlag(CurrentRecomendations.Values))
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

                // TODO: support out once added
                if (param.Modifiers.HasFlag(Modifiers.Ref))
                    builder.Append("ref ");
            }

            return builder
                .Append(')')
                .ToString();
        }

        private CurrentRecomendations getRecomendation(SyntaxNode node)
        {
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

            CurrentRecomendations? recomendation = getRecomendationWithParent(node, parent);

            return recomendation ?? CurrentRecomendations.All;
        }

        private CurrentRecomendations? getRecomendationWithParent(SyntaxNode node, SyntaxNode parent)
        {
            switch (parent)
            {
                case NameExpressionSyntax:
                    {
                        if (parent.Parent is not null)
                            return getRecomendationWithParent(parent, parent.Parent);
                    }
                    break;
                case CallExpressionSyntax call:
                    {
                        if (node == call.Identifier)
                            return CurrentRecomendations.Functions;
                    }
                    break;
                case ArgumentClauseSyntax argumentClause: // TODO: once (out float val) is added, add | CurrentRecomendations.Types
                    return CurrentRecomendations.InExpression | CurrentRecomendations.Modifiers;
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
                case ArrayInitializerStatementSyntax arrayInitializer when node != arrayInitializer.IdentifierToken:
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
