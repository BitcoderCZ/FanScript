using MathUtils.Vectors;

namespace FanScript.FCInfo
{
    public class Block
    {
        public Vector3I Pos;
        public readonly BlockDef Type;

        public Block(Vector3I _pos, BlockDef _type)
        {
            Pos = _pos;
            Type = _type;
        }

        public override string ToString()
            => $"{{Pos: {Pos}, Type: {Type}}}";

        public override int GetHashCode()
            => Pos.GetHashCode() ^ Type.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (obj is Block other)
                return Pos == other.Pos && Type == other.Type;
            else
                return false;
        }
    }
}
