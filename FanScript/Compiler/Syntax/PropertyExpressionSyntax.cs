namespace FanScript.Compiler.Syntax
{
    public sealed partial class PropertyExpressionSyntax : ExpressionSyntax
    {
        internal PropertyExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression, SyntaxToken dotToken, SyntaxToken identifierToken) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            DotToken = dotToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.PropertyExpression;

        public ExpressionSyntax Expression { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken IdentifierToken { get; }
    }
}
