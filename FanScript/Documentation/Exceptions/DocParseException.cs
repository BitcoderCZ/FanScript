namespace FanScript.Documentation.Exceptions;

public abstract class DocParseException : Exception
{
    protected DocParseException(string message)
        : base(message)
    {
    }
}
