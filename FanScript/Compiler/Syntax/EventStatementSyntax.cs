namespace FanScript.Compiler.Syntax;

public sealed partial class EventStatementSyntax : StatementSyntax
{
    internal EventStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken, SyntaxToken identifier, ArgumentClauseSyntax? argumentClause, BlockStatementSyntax block)
        : base(syntaxTree)
    {
        KeywordToken = keywordToken;
        Identifier = identifier;
        ArgumentClause = argumentClause;
        Block = block;
    }

    public override SyntaxKind Kind => SyntaxKind.EventStatement;

    public SyntaxToken KeywordToken { get; }

    public SyntaxToken Identifier { get; }

    public ArgumentClauseSyntax? ArgumentClause { get; }

    public BlockStatementSyntax Block { get; }
}
