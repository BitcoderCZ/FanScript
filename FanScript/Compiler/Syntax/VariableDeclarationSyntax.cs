namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationSyntax : StatementSyntax
    {
        internal VariableDeclarationSyntax(SyntaxTree syntaxTree, TypeClauseSyntax typeClause, SyntaxToken identifier, AssignmentStatementSyntax? optionalAssignment)
            : base(syntaxTree)
        {
            TypeClause = typeClause;
            Identifier = identifier;
            OptionalAssignment = optionalAssignment;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken Identifier { get; }
        public AssignmentStatementSyntax? OptionalAssignment { get; }
    }
}
