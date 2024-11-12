namespace FanScript.Documentation.Exceptions;

public sealed class DuplicateElementArgException : DocParseException
{
    public DuplicateElementArgException(string elementName, string argName)
        : base($"Arg \"{argName}\" is multiple times in element \"{elementName}\".")
    {
    }
}
