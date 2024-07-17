namespace FanScript.Compiler.Syntax
{
    public sealed partial class SpecialBlockStatementSyntax : StatementSyntax
    {
        public SpecialBlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken, SyntaxToken identifier, SyntaxToken openParenthesisToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenthesisToken, BlockStatementSyntax block)
            : base(syntaxTree)
        {
            KeywordToken = keywordToken;
            Identifier = identifier;
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
            Block = block;
        }

        public override SyntaxKind Kind => SyntaxKind.SpecialBlockStatement;
        public SyntaxToken KeywordToken { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }
        public BlockStatementSyntax Block { get; }
    }
}
