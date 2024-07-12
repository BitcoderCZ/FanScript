using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class CallExpressionSyntax : ExpressionSyntax
    {
        internal CallExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifier, SyntaxToken openParenthesisToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenthesisToken)
            : base(syntaxTree)
        {
            Identifier = identifier;
            HasGenericParameter = false;
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }
        internal CallExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifier, SyntaxToken lessThanToken, TypeClauseSyntax genericTypeClause, SyntaxToken greaterThanToken, SyntaxToken openParenthesisToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenthesisToken)
            : base(syntaxTree)
        {
            Identifier = identifier;
            HasGenericParameter = true;
            LessThanToken = lessThanToken;
            GenericTypeClause = genericTypeClause;
            GreaterThanToken = greaterThanToken;
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }

        public override SyntaxKind Kind => SyntaxKind.CallExpression;
        public SyntaxToken Identifier { get; }
        [MemberNotNullWhen(true, nameof(LessThanToken), nameof(GenericTypeClause), nameof(GreaterThanToken))]
        public bool HasGenericParameter { get; }
        public SyntaxToken? LessThanToken { get; }
        public TypeClauseSyntax? GenericTypeClause { get; }
        public SyntaxToken? GreaterThanToken { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }
    }
}
