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
    }
}
