using FanScript.Compiler.Text;

namespace FanScript.Compiler.Syntax
{
    public sealed class SyntaxTrivia
    {
        internal SyntaxTrivia(SyntaxTree syntaxTree, SyntaxKind kind, int position, string text)
        {
            SyntaxTree = syntaxTree;
            Kind = kind;
            Position = position;
            Text = text;
        }

        public SyntaxTree SyntaxTree { get; }
        public SyntaxKind Kind { get; }
        public int Position { get; }
        public TextSpan Span => new TextSpan(Position, Text?.Length ?? 0);
        public string Text { get; }
    }
}
