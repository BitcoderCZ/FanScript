namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationSyntax : StatementSyntax
    {
        internal VariableDeclarationSyntax(SyntaxTree syntaxTree, SyntaxToken keyword, SyntaxToken identifier, AssignmentStatementSyntax? optionalAssignment)
            : base(syntaxTree)
        {
            Keyword = keyword;
            Identifier = identifier;
            OptionalAssignment = optionalAssignment;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;
        public SyntaxToken Keyword { get; }
        public SyntaxToken Identifier { get; }
        public AssignmentStatementSyntax? OptionalAssignment { get; }
    }
}
