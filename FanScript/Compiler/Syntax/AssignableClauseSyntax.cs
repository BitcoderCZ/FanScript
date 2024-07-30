namespace FanScript.Compiler.Syntax
{
    public abstract class AssignableClauseSyntax : SyntaxNode
    {
        protected internal AssignableClauseSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }

        public override SyntaxKind Kind => SyntaxKind.AssignableClause;
    }

    public sealed partial class AssignableVariableClauseSyntax : AssignableClauseSyntax
    {
        internal AssignableVariableClauseSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
        }

        public SyntaxToken IdentifierToken { get; }
    }

    public sealed partial class AssignablePropertyClauseSyntax : AssignableClauseSyntax
    {
        internal AssignablePropertyClauseSyntax(SyntaxTree syntaxTree, SyntaxToken variableToken, SyntaxToken dotToken, SyntaxToken propertyToken)
            : base(syntaxTree)
        {
            VariableToken = variableToken;
            DotToken = dotToken;
            IdentifierToken = propertyToken;
        }

        public SyntaxToken VariableToken { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken IdentifierToken { get; }
    }
}
