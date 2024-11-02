using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler
{
    public class Block
    {
        public readonly BlockDef Type;
        public Vector3I Pos;

        public Block(Vector3I pos, BlockDef type)
        {
            Pos = pos;
            Type = type;
        }

        public override string ToString()
            => $"{{Pos: {Pos}, Type: {Type}}}";

        public override int GetHashCode()
            => Pos.GetHashCode() ^ Type.GetHashCode();

        public override bool Equals(object? obj)
            => obj is Block other && Pos == other.Pos && Type == other.Type;
    }
}
