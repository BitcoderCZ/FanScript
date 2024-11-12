namespace FanScript.Compiler.Syntax;

public abstract class AssignableExpressionSyntax : ExpressionSyntax
{
    private protected AssignableExpressionSyntax(SyntaxTree syntaxTree)
        : base(syntaxTree)
    {
    }
}
