namespace FanScript.Compiler.Syntax
{
    public sealed partial class ArrayInitializerStatementSyntax : StatementSyntax
    {
        internal ArrayInitializerStatementSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken equalsToken, SyntaxToken openSquareToken, SeparatedSyntaxList<ExpressionSyntax> elements, SyntaxToken closeSquareToken)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            OpenSquareToken = openSquareToken;
            Elements = elements;
            CloseSquareToken = closeSquareToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ArrayInitializerStatement;

        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public SyntaxToken OpenSquareToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Elements { get; }
        public SyntaxToken CloseSquareToken { get; }
    }
}
