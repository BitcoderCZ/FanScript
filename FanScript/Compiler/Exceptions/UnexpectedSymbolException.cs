using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Exceptions;

public sealed class UnexpectedSymbolException : Exception
{
    public UnexpectedSymbolException(Symbol symbol)
        : base($"Unexpected symbol '{symbol?.GetType()?.FullName ?? "null"}'.")
    {
    }
}
