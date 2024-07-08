using MathUtils.Vectors;

namespace FanScript.FCInfo
{
    public class Terminal
    {
        public readonly int Index;
        public readonly WireType WireType;
        public readonly TerminalType Type;
        public readonly string Name;
        public Vector3I Pos { get; internal set; }

        public Terminal(int _index, WireType _wireType, TerminalType _type, string _name)
        {
            Index = _index;
            WireType = _wireType;
            Type = _type;
            Name = _name;
        }
        public Terminal(int _index, WireType _wireType, TerminalType _type)
            : this(_index, _wireType, _type, string.Empty)
        {
        }
    }
}
