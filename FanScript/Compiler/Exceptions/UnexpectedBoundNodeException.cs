using FanScript.Compiler.Binding;

namespace FanScript.Compiler.Exceptions;

public sealed class UnexpectedBoundNodeException : Exception
{
    internal UnexpectedBoundNodeException(BoundNode node)
        : base($"Unexpected bound node '{node?.GetType()?.FullName ?? "null"}'.")
    {
    }
}
