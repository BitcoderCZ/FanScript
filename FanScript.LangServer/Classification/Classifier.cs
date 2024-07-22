using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Immutable;

namespace FanScript.LangServer.Classification
{
    public static class Classifier
    {
        public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span)
        {
            var result = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, result);
            return result.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (node is null || !node.FullSpan.OverlapsWith(span))
                return;

            // TODO: get parent and compare node with it's properties to get better classification, for ex: if parent is CallExpressionSyntax and node == Parent.Identifier, return SemanticTokenType.Function
            if (node is SyntaxToken token)
                ClassifyToken(token, span, result);

            foreach (var child in node.GetChildren())
                ClassifyNode(child, span, result);
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            foreach (var leadingTrivia in token.LeadingTrivia)
                ClassifyTrivia(leadingTrivia, span, result);

            AddClassification(token.Kind, token.Span, span, result);

            foreach (var trailingTrivia in token.TrailingTrivia)
                ClassifyTrivia(trailingTrivia, span, result);
        }

        private static void ClassifyTrivia(SyntaxTrivia trivia, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            AddClassification(trivia.Kind, trivia.Span, span, result);
        }

        private static void AddClassification(SyntaxKind elementKind, TextSpan elementSpan, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!elementSpan.OverlapsWith(span))
                return;

            SemanticTokenType? classification = GetClassification(elementKind);
            if (classification is null)
                return;

            int adjustedStart = Math.Max(elementSpan.Start, span.Start);
            int adjustedEnd = Math.Min(elementSpan.End, span.End);
            TextSpan adjustedSpan = TextSpan.FromBounds(adjustedStart, adjustedEnd);

            result.Add(new ClassifiedSpan(adjustedSpan, classification.Value));
        }

        private static SemanticTokenType? GetClassification(SyntaxKind kind)
        {
            // TODO: add kind.IsOperand SemanticTokenType.Operator (+, -, ...)

            if (kind.IsKeyword())
                return SemanticTokenType.Keyword;
            else if (kind.IsModifier())
                return SemanticTokenType.Modifier;
            else if (kind == SyntaxKind.IdentifierToken)
                return SemanticTokenType.Variable;
            else if (kind == SyntaxKind.FloatToken)
                return SemanticTokenType.Number;
            else if (kind.IsComment())
                return SemanticTokenType.Comment;
            else
                return null;
        }
    }
}
