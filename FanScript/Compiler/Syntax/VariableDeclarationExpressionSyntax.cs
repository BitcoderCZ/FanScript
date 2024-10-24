using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class VariableDeclarationExpressionSyntax : ExpressionSyntax
    {
        internal VariableDeclarationExpressionSyntax(SyntaxTree syntaxTree, ModifierClauseSyntax modifierClause, TypeClauseSyntax typeClause, SyntaxToken identifierToken)
           : base(syntaxTree)
        {
            ModifierClause = new ModifierClauseSyntax(
                syntaxTree, 
                modifierClause.Modifiers
                    .Where(token => ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                    .ToImmutableArray()
            );
            TypeClause = typeClause;
            IdentifierToken = identifierToken;
        }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationExpression;

        public ModifierClauseSyntax ModifierClause { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken IdentifierToken { get; }
    }
}
