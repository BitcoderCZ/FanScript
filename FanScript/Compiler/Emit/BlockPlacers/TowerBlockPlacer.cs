using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class TowerBlockPlacer : IBlockPlacer
    {
        public int CurrentCodeBlockBlocks => blockCount;

        public int MaxHeight { get; set; } = 10;
        public Move NextTowerMove { get; set; } = Move.Right;

        private Vector3I pos = Vector3I.Zero;

        private int y = 0;
        private int blockCount = 0;

        public Vector3I Place(DefBlock defBlock)
        {
            const int move = 4;

            blockCount++;
            Vector3I _pos = pos;
            pos.Y++;

            if (pos.Y > MaxHeight)
            {
                pos.Y = 0;
                switch (NextTowerMove)
                {
                    case Move.Forwards:
                        pos.Z += move;
                        break;
                    case Move.Backwards:
                        pos.Z -= move;
                        break;
                    case Move.Left:
                        pos.X -= move;
                        break;
                    case Move.Right:
                        pos.X += move;
                        break;
                    default:
                        throw new InvalidDataException($"Unknown Move '{NextTowerMove}'");
                }
            }

            return _pos;
        }

        public void EnterStatementBlock()
        {
        }
        public void ExitStatementBlock()
        {
        }

        public void EnterExpression()
        {
        }
        public void ExitExpression()
        {
        }

        public enum Move
        {
            Forwards,
            Backwards,
            Left,
            Right,
        }
    }
}
