namespace FanScript.Documentation.Exceptions
{
    public sealed class InvalidElementArgValueException : DocParseException
    {
        public InvalidElementArgValueException(string elementName, string argName)
            : base($"Arg \"{argName}\" in element \"{elementName}\" has invalid value.")
        {
        }
    }
}
