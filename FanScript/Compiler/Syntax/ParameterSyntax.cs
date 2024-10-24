namespace FanScript.Compiler.Syntax
{
    public sealed partial class ParameterSyntax : SyntaxNode
    {
        internal ParameterSyntax(SyntaxTree syntaxTree, ModifierClauseSyntax modifiers, TypeClauseSyntax typeClause, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Modifiers = modifiers;
            TypeClause = typeClause;
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.Parameter;

        public ModifierClauseSyntax Modifiers { get; }
        public TypeClauseSyntax TypeClause { get; }
        public SyntaxToken Identifier { get; }
    }
}
