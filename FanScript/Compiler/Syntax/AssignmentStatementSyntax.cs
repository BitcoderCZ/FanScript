namespace FanScript.Compiler.Syntax
{
    public sealed partial class AssignmentStatementSyntax : StatementSyntax
    {
        public AssignmentStatementSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken assignmentToken, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            AssignmentToken = assignmentToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken AssignmentToken { get; }
        public ExpressionSyntax Expression { get; }
    }
}
