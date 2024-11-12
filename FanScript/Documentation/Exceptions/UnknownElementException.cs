namespace FanScript.Documentation.Exceptions;

public sealed class UnknownElementException : DocParseException
{
    public UnknownElementException(string elementName)
        : base($"Unknown element \"{elementName}\".")
    {
    }
}
