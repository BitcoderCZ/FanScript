using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ModifierClauseSyntax : SyntaxNode
    {
        public ModifierClauseSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, ExpressionSyntax expression) : base(syntaxTree)
        {
            Modifiers = modifiers;
            Expression = expression;
        }

        public override SyntaxKind Kind => throw new NotImplementedException();

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public ExpressionSyntax Expression { get; }
    }
}
