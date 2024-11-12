using MathUtils.Vectors;

namespace FanScript.FCInfo;

public class Terminal
{
    public readonly WireType WireType;
    public readonly TerminalType Type;
    public readonly string Name;

    private bool _initialized;

    public Terminal(WireType wireType, TerminalType type, string name)
    {
        WireType = wireType;
        Type = type;
        Name = name;
    }

    public Terminal(WireType wireType, TerminalType type)
        : this(wireType, type, string.Empty)
    {
    }

    public int Index { get; private set; }

    public Vector3I Pos { get; private set; }

    internal void Init(int index, Vector3I pos)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Already initialized.");
        }

        _initialized = true;

        Index = index;
        Pos = pos;
    }
}
