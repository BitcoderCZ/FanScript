using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ModifierClauseSyntax : SyntaxNode
    {
        internal ModifierClauseSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, ExpressionSyntax expression) : base(syntaxTree)
        {
            Modifiers = modifiers;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.ModifierClause;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public ExpressionSyntax Expression { get; }
    }
}
