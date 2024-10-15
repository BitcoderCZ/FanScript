namespace FanScript.Documentation.Exceptions
{
    public sealed class ElementArgValueMissingException : DocParseException
    {
        public ElementArgValueMissingException(string elementName, string argName)
            : base($"Required value of arg \"{argName}\" in element \"{argName}\" is missing.")
        {
        }
    }
}
