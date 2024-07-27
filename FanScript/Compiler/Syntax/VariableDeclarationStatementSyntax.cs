using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationStatementSyntax : StatementSyntax
    {
        internal VariableDeclarationStatementSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, TypeClauseSyntax typeClause, SyntaxToken identifierToken, StatementSyntax? optionalAssignment)
            : base(syntaxTree)
        {
            Modifiers = modifiers;
            TypeClause = typeClause;
            IdentifierToken = identifierToken;
            OptionalAssignment = optionalAssignment;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken IdentifierToken { get; }
        public StatementSyntax? OptionalAssignment { get; }
    }
}
