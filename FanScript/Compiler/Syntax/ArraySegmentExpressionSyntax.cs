namespace FanScript.Compiler.Syntax
{
    public sealed partial class ArraySegmentExpressionSyntax : ExpressionSyntax
    {
        internal ArraySegmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openSquareToken, SeparatedSyntaxList<ExpressionSyntax> elements, SyntaxToken closeSquareToken) : base(syntaxTree)
        {
            OpenSquareToken = openSquareToken;
            Elements = elements;
            CloseSquareToken = closeSquareToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ArraySegmentExpression;

        public SyntaxToken OpenSquareToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Elements { get; }
        public SyntaxToken CloseSquareToken { get; }
    }
}
