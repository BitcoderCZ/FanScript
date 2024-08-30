using FanScript.FCInfo;
using MathUtils.Vectors;
using System.ComponentModel;
using System.Diagnostics;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class TowerBlockPlacer : IBlockPlacer
    {
        public int CurrentCodeBlockBlocks => blocks.Count;

        private int maxHeight = 20;
        public int MaxHeight
        {
            get => maxHeight;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                maxHeight = value;
            }
        }
        public Move NextTowerMove { get; set; } = Move.X;
        public bool SquarePlacement { get; set; } = true;

        private Vector3I pos = Vector3I.Zero;

        private int statementDepth;

        private List<Block> blocks = new List<Block>(256);

        public Block Place(BlockDef blockDef)
        {
            Block block = new Block(pos, blockDef);
            blocks.Add(block);
            return block;
        }

        public void EnterStatementBlock()
        {
            statementDepth++;
        }
        public void ExitStatementBlock()
        {
            const int move = 4;

            statementDepth--;

            Debug.Assert(statementDepth >= 0);

            if (statementDepth == 0)
            {
                // https://stackoverflow.com/a/17974
                int width = (blocks.Count + MaxHeight - 1) / MaxHeight;

                if (SquarePlacement)
                    width = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(width)));

                width *= move;
                int off = NextTowerMove == Move.X ? pos.X : 0;

                Vector3I bPos = pos;

                for (int i = 0; i < blocks.Count; i++)
                {
                    blocks[i].Pos = bPos;
                    bPos.Y++;

                    if (bPos.Y > MaxHeight)
                    {
                        bPos.Y = 0;
                        bPos.X += move;

                        if (bPos.X >= width + off)
                        {
                            bPos.X = off;
                            bPos.Z += move;
                        }
                    }
                }

                switch (NextTowerMove)
                {
                    case Move.X:
                        pos.X += width + 4;
                        break;
                    case Move.Z:
                        pos.Z = bPos.Z + 4;
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(NextTowerMove), (int)NextTowerMove, typeof(Move));
                }

                blocks.Clear();
            }
        }

        public void EnterExpressionBlock()
        {
        }
        public void ExitExpressionBlock()
        {
        }

        public enum Move
        {
            X,
            Z
        }
    }
}
