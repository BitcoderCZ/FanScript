namespace FanScript.Compiler.Syntax;

public abstract class StatementSyntax : SyntaxNode
{
    private protected StatementSyntax(SyntaxTree syntaxTree)
        : base(syntaxTree)
    {
    }
}
