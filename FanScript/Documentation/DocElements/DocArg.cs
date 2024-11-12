namespace FanScript.Documentation.DocElements;

public readonly struct DocArg
{
    public readonly string Name;
    public readonly string? Value;

    public DocArg(string name, string? value)
    {
        Name = name;
        Value = value;
    }
}
