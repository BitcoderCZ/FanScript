using System.Collections.Immutable;
using FanScript.Compiler.Text;

namespace FanScript.Compiler.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        internal SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position, string? text, object? value, ImmutableArray<SyntaxTrivia> leadingTrivia, ImmutableArray<SyntaxTrivia> trailingTrivia)
            : base(syntaxTree)
        {
            Kind = kind;
            Position = position;
            Text = text ?? string.Empty;
            IsMissing = text is null;
            Value = value;
            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;
        }

        public override SyntaxKind Kind { get; }

        public int Position { get; }

        public string Text { get; }

        public object? Value { get; }

        public override TextSpan Span => new TextSpan(Position, Text.Length);

        public override TextSpan FullSpan
        {
            get
            {
                int start = LeadingTrivia.Length == 0
                                ? Span.Start
                                : LeadingTrivia.First().Span.Start;
                int end = TrailingTrivia.Length == 0
                                ? Span.End
                                : TrailingTrivia.Last().Span.End;
                return TextSpan.FromBounds(start, end);
            }
        }

        public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }

        public ImmutableArray<SyntaxTrivia> TrailingTrivia { get; }

        /// <summary>
        /// A token is missing if it was inserted by the parser and doesn't appear in source.
        /// </summary>
        public bool IsMissing { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
            => [];
    }
}
