namespace FanScript.Compiler.Syntax
{
    public sealed partial class SpecialBlockStatementSyntax : StatementSyntax
    {
        internal SpecialBlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken, SyntaxToken identifier, ArgumentClauseSyntax? argumentClause, BlockStatementSyntax block)
            : base(syntaxTree)
        {
            KeywordToken = keywordToken;
            Identifier = identifier;
            ArgumentClause = argumentClause;
            Block = block;
        }

        public override SyntaxKind Kind => SyntaxKind.SpecialBlockStatement;
        public SyntaxToken KeywordToken { get; }
        public SyntaxToken Identifier { get; }
        public ArgumentClauseSyntax? ArgumentClause { get; }
        public BlockStatementSyntax Block { get; }
    }
}
