using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ParameterSyntax : SyntaxNode
    {
        internal ParameterSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, TypeClauseSyntax typeClause, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Modifiers = modifiers;
            TypeClause = typeClause;
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.Parameter;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken Identifier { get; }
    }
}
