using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class CallStatementSyntax : StatementSyntax
    {
        internal CallStatementSyntax(SyntaxTree syntaxTree, SyntaxToken identifier, ArgumentClauseSyntax argumentClause)
            : base(syntaxTree)
        {
            Identifier = identifier;
            HasGenericParameter = false;
            ArgumentClause = argumentClause;
        }

        internal CallStatementSyntax(SyntaxTree syntaxTree, SyntaxToken identifier, SyntaxToken lessThanToken, TypeClauseSyntax genericTypeClause, SyntaxToken greaterThanToken, ArgumentClauseSyntax argumentClause)
            : base(syntaxTree)
        {
            Identifier = identifier;
            HasGenericParameter = true;
            LessThanToken = lessThanToken;
            GenericTypeClause = genericTypeClause;
            GreaterThanToken = greaterThanToken;
            ArgumentClause = argumentClause;
        }

        public override SyntaxKind Kind => SyntaxKind.CallStatement;

        public SyntaxToken Identifier { get; }

        [MemberNotNullWhen(true, nameof(LessThanToken), nameof(GenericTypeClause), nameof(GreaterThanToken))]
        public bool HasGenericParameter { get; }

        public SyntaxToken? LessThanToken { get; }

        public TypeClauseSyntax? GenericTypeClause { get; }

        public SyntaxToken? GreaterThanToken { get; }

        public ArgumentClauseSyntax ArgumentClause { get; }

        public SeparatedSyntaxList<ModifiersWExpressionSyntax> Arguments => ArgumentClause.Arguments;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            if (HasGenericParameter)
            {
                yield return LessThanToken;
                yield return GenericTypeClause;
                yield return GreaterThanToken;
            }

            yield return ArgumentClause;
        }
    }
}
