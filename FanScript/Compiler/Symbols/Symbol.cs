using System.Diagnostics;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols;

public abstract class Symbol : ITextWritable
{
    private protected Symbol(string name)
    {
        Debug.Assert(!string.IsNullOrEmpty(name), "Symbol name cannot be empty.");
        Name = name;
    }

    public abstract SymbolKind Kind { get; }

    public string Name { get; protected set; }

    public abstract void WriteTo(TextWriter writer);

    public override string ToString()
    {
        using (var writer = new StringWriter())
        {
            WriteTo(writer);
            return writer.ToString();
        }
    }

    public override int GetHashCode()
        => Name.GetHashCode();

    public override bool Equals(object? obj)
        => obj is Symbol other && Name == other.Name;
}
