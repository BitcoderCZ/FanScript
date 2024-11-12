namespace FanScript.Compiler.Syntax;

public abstract class ExpressionSyntax : SyntaxNode
{
    private protected ExpressionSyntax(SyntaxTree syntaxTree)
        : base(syntaxTree)
    {
    }
}
