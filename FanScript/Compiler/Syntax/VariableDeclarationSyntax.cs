namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationSyntax : StatementSyntax
    {
        internal VariableDeclarationSyntax(SyntaxTree syntaxTree, TypeClauseSyntax typeClause, SyntaxToken identifier, StatementSyntax? optionalAssignment)
            : base(syntaxTree)
        {
            TypeClause = typeClause;
            Identifier = identifier;
            OptionalAssignment = optionalAssignment;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken Identifier { get; }
        public StatementSyntax? OptionalAssignment { get; }
    }
}
