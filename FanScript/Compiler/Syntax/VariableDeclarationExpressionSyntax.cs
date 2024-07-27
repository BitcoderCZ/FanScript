using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationExpressionSyntax : ExpressionSyntax
    {
        internal VariableDeclarationExpressionSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, TypeClauseSyntax typeClause, SyntaxToken identifierToken)
           : base(syntaxTree)
        {
            Modifiers = modifiers
                .Where(token => ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                .ToImmutableArray();
            TypeClause = typeClause;
            IdentifierToken = identifierToken;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationExpression;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken IdentifierToken { get; }
    }
}
