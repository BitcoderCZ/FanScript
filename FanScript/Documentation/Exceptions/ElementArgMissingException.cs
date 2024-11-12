namespace FanScript.Documentation.Exceptions;

public sealed class ElementArgMissingException : DocParseException
{
    public ElementArgMissingException(string elementName, string argName)
        : base($"Required arg \"{argName}\" is missing in element \"{elementName}\".")
    {
    }
}
