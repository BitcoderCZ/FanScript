using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.FCInfo
{
    public class Block
    {
        public Vector3I Pos;
        public readonly DefBlock Type;

        public Block(Vector3I _pos, DefBlock _type)
        {
            Pos = _pos;
            Type = _type;
        }
    }
}
