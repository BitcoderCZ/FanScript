using System.Diagnostics;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit.CodePlacers;

public class TowerCodePlacer : CodePlacer
{
    private readonly List<Block> _blocks = new List<Block>(256);

    private int _maxHeight = 20;
    private bool _inHighlight = false;
    private int _statementDepth = 0;

    public TowerCodePlacer(BlockBuilder builder)
        : base(builder)
    {
    }

    public enum Move
    {
        X,
        Z,
    }

    public override int CurrentCodeBlockBlocks => _blocks.Count;

    public int MaxHeight
    {
        get => _maxHeight;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxHeight = value;
        }
    }

    public bool SquarePlacement { get; set; } = true;

    public override Block PlaceBlock(BlockDef blockDef)
    {
        Block block;

        if (_inHighlight)
        {
            block = new Block(Vector3I.Zero, blockDef);
            Builder.AddHighlightedBlock(block);
        }
        else
        {
            block = new Block(Vector3I.Zero, blockDef);
            _blocks.Add(block);
        }

        return block;
    }

    public override void EnterStatementBlock()
        => _statementDepth++;

    public override void ExitStatementBlock()
    {
        const int move = 4;

        _statementDepth--;

        Debug.Assert(_statementDepth >= 0, "Must be in a statement to exit one.");

        if (_statementDepth == 0 && _blocks.Count > 0)
        {
            // https://stackoverflow.com/a/17974
            int width = (_blocks.Count + MaxHeight - 1) / MaxHeight;

            if (SquarePlacement)
            {
                width = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(width)));
            }

            width *= move;

            Vector3I bPos = Vector3I.Zero;

            for (int i = 0; i < _blocks.Count; i++)
            {
                _blocks[i].Pos = bPos;
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

            Builder.AddBlockSegments(_blocks);

            _blocks.Clear();
        }
    }

    public override void EnterExpressionBlock()
    {
    }

    public override void ExitExpressionBlock()
    {
    }

    public override void EnterHighlight()
        => _inHighlight = true;

    public override void ExitHightlight()
        => _inHighlight = false;
}
