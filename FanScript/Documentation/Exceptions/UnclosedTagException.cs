namespace FanScript.Documentation.Exceptions
{
    public abstract class UnclosedTagException : DocParseException
    {
        protected UnclosedTagException(string message)
            : base(message)
        {
        }
    }

    public sealed class UnclosedStartTagException : UnclosedTagException
    {
        public UnclosedStartTagException(string elementName)
            : base($"An element \"{elementName}\" has unclosed start tag.")
        {
        }
    }

    public sealed class UnclosedEndTagException : UnclosedTagException
    {
        public UnclosedEndTagException(string elementName)
            : base($"An element \"{elementName}\" has unclosed end tag.")
        {
        }
    }
}
