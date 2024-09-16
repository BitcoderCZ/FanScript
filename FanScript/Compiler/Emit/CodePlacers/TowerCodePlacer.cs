using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class TowerCodePlacer : CodePlacer
    {
        public override int CurrentCodeBlockBlocks => blocks.Count;

        private int maxHeight = 25;
        public int MaxHeight
        {
            get => maxHeight;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                maxHeight = value;
            }
        }
        public bool SquarePlacement { get; set; } = true;

        private int highlightX = 0;

        private bool inHighlight = false;
        private int statementDepth = 0;

        private List<Block> blocks = new List<Block>(256);

        public TowerCodePlacer(BlockBuilder builder)
            : base(builder)
        {
        }

        public override Block PlaceBlock(BlockDef blockDef)
        {
            Block block;

            if (inHighlight)
            {
                block = new Block(new Vector3I(highlightX, 0, -4), blockDef);
                highlightX += blockDef.Size.X + 1;
            }
            else
            {
                block = new Block(Vector3I.Zero, blockDef);
                blocks.Add(block);
            }

            return block;
        }

        public override void EnterStatementBlock()
        {
            statementDepth++;
        }
        public override void ExitStatementBlock()
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

                Vector3I bPos = Vector3I.Zero;

                for (int i = 0; i < blocks.Count; i++)
                {
                    blocks[i].Pos = bPos;
                    bPos.Y++;

                    if (bPos.Y > MaxHeight)
                    {
                        bPos.Y = 0;
                        bPos.X += move;

                        if (bPos.X >= width)
                        {
                            bPos.X = 0;
                            bPos.Z += move;
                        }
                    }
                }

                Builder.AddBlockSegments(blocks);

                blocks.Clear();
            }
        }

        public override void EnterExpressionBlock()
        {
        }
        public override void ExitExpressionBlock()
        {
        }

        public override void EnterHighlight()
        {
            inHighlight = true;
        }

        public override void ExitHightlight()
        {
            if (inHighlight)
                highlightX += 2;

            inHighlight = false;
        }

        public enum Move
        {
            X,
            Z
        }
    }
}
