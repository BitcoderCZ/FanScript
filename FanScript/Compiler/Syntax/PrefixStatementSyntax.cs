namespace FanScript.Compiler.Syntax
{
    public sealed partial class PrefixStatementSyntax : StatementSyntax
    {
        internal PrefixStatementSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            OperatorToken = operatorToken;
            IdentifierToken = identifierToken;
        }

        public override SyntaxKind Kind => SyntaxKind.PrefixStatement;

        public SyntaxToken OperatorToken { get; }

        public SyntaxToken IdentifierToken { get; }
    }
}
