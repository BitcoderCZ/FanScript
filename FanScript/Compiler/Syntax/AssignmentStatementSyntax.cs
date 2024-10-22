namespace FanScript.Compiler.Syntax
{
    public sealed partial class AssignmentStatementSyntax : StatementSyntax
    {
        internal AssignmentStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax destination, SyntaxToken assignmentToken, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Destination = destination;
            AssignmentToken = assignmentToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;
        public ExpressionSyntax Destination { get; }
        public SyntaxToken AssignmentToken { get; }
        public ExpressionSyntax Expression { get; }
    }
}
