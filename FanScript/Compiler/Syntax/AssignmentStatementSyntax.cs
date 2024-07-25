namespace FanScript.Compiler.Syntax
{
    public sealed partial class AssignmentStatementSyntax : StatementSyntax
    {
        public AssignmentStatementSyntax(SyntaxTree syntaxTree, AssignableClauseSyntax assignableClause, SyntaxToken assignmentToken, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            AssignableClause = assignableClause;
            AssignmentToken = assignmentToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;
        public AssignableClauseSyntax AssignableClause { get; }
        public SyntaxToken AssignmentToken { get; }
        public ExpressionSyntax Expression { get; }
    }
}
