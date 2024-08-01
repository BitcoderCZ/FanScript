using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class TypeClauseSyntax : SyntaxNode
    {
        internal TypeClauseSyntax(SyntaxTree syntaxTree, SyntaxToken typeToken)
            : base(syntaxTree)
        {
            TypeToken = typeToken;
            HasGenericParameter = false;
        }
        internal TypeClauseSyntax(SyntaxTree syntaxTree, SyntaxToken typeToken, SyntaxToken lessToken, TypeClauseSyntax innerType, SyntaxToken greaterToken)
            : base(syntaxTree)
        {
            TypeToken = typeToken;
            HasGenericParameter = true;
            LessToken = lessToken;
            InnerType = innerType;
            GreaterToken = greaterToken;
        }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        public SyntaxToken TypeToken { get; }
        [MemberNotNullWhen(true, nameof(LessToken), nameof(InnerType), nameof(GreaterToken))]
        public bool HasGenericParameter { get; }
        public SyntaxToken? LessToken { get; }
        public TypeClauseSyntax? InnerType { get; }
        public SyntaxToken? GreaterToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeToken;
            if (HasGenericParameter)
            {
                yield return LessToken;
                yield return InnerType;
                yield return GreaterToken;
            }
        }
    }
}
