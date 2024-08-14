using MathUtils.Vectors;

namespace FanScript.FCInfo
{
    public class Terminal
    {
        public readonly WireType WireType;
        public readonly TerminalType Type;
        public readonly string Name;

        private bool initialized;
        public int Index { get; private set; }
        public Vector3I Pos { get; private set; }

        public Terminal(WireType _wireType, TerminalType _type, string _name)
        {
            WireType = _wireType;
            Type = _type;
            Name = _name;
        }
        public Terminal(WireType _wireType, TerminalType _type)
            : this(_wireType, _type, string.Empty)
        {
        }

        internal void Init(int index, Vector3I pos)
        {
            if (initialized)
                throw new InvalidOperationException("Already initialized");

            initialized = true;

            Index = index;
            Pos = pos;
        }
    }
}
