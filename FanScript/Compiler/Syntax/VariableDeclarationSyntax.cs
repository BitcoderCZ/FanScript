using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationSyntax : StatementSyntax
    {
        internal VariableDeclarationSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, TypeClauseSyntax typeClause, SyntaxToken identifier, StatementSyntax? optionalAssignment)
            : base(syntaxTree)
        {
            Modifiers = modifiers;
            TypeClause = typeClause;
            Identifier = identifier;
            OptionalAssignment = optionalAssignment;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken Identifier { get; }
        public StatementSyntax? OptionalAssignment { get; }
    }
}
