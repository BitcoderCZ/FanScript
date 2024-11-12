using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Exceptions;

public sealed class UnexpectedSyntaxException : Exception
{
    public UnexpectedSyntaxException(SyntaxNode node)
        : base($"Unexpected syntax '{node?.GetType()?.FullName ?? "null"}'.")
    {
    }
}
