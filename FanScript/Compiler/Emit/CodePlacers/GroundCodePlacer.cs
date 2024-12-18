﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit.CodePlacers;

public class GroundCodePlacer : CodePlacer
{
    protected readonly Stack<StatementCodeBlock> statements = new();
    protected readonly Stack<ExpressionCodeBlock> expressions = new();

    protected bool inHighlight = false;

    private int _blockXOffset = 3;

    public GroundCodePlacer(BlockBuilder builder)
        : base(builder)
    {
    }

    public override int CurrentCodeBlockBlocks => expressions.Count != 0 ? expressions.Peek().BlockCount : statements.Peek().BlockCount;

    /// <summary>
    /// Horizontal space between blocks of code (min value and default is <see langword="3"/>)
    /// </summary>
    public int BlockXOffset
    {
        get => _blockXOffset;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 3, nameof(BlockXOffset));
            _blockXOffset = value;
        }
    }

    public override Block PlaceBlock(BlockDef blockDef)
    {
        Block block;

        if (inHighlight)
        {
            block = new Block(Vector3I.Zero, blockDef);
            Builder.AddHighlightedBlock(block);
        }
        else
        {
            block = expressions.Count != 0 ? expressions.Peek().PlaceBlock(blockDef) : statements.Peek().PlaceBlock(blockDef);
        }

        return block;
    }

    public override void EnterStatementBlock()
    {
        Debug.Assert(expressions.Count == 0, "Cannot enter a statement block while being in an expression block");

        if (statements.Count == 0)
        {
            statements.Push(CreateFunction());
        }
        else
        {
            statements.Push(statements.Peek().CreateStatementChild());
        }
    }

    public override void ExitStatementBlock()
    {
        Debug.Assert(expressions.Count == 0, "Cannot exit a statement block while being in an expression block");

        StatementCodeBlock statement = statements.Pop();

        if (statements.Count == 0 && statement.AllBlocks.Any())
        {
            // end of function
            LayerStack.Process(statement, BlockXOffset);

            Builder.AddBlockSegments(statement.AllBlocks);
        }
    }

    public override void EnterExpressionBlock()
    {
        Debug.Assert(statements.Count > 0, "Cannot enter an expression block without being in a statement block");

        if (expressions.Count == 0)
        {
            expressions.Push(statements.Peek().CreateExpressionChild());
        }
        else
        {
            expressions.Push(expressions.Peek().CreateExpressionChild());
        }
    }

    public override void ExitExpressionBlock()
    {
        Debug.Assert(statements.Count > 0, "Cannot exit an expression block without being in a statement block");

        ExpressionCodeBlock expression = expressions.Pop();

        if (statements.Count == 1)
        {
            if (expression.Parent!.Parent is not null)
            {
                throw new Exception("Parent not null.");
            }

            // if this is child of the top statement, process and assign x and y position to blocks
            LayerStack.Process(expression, BlockXOffset);
        }
    }

    public override void EnterHighlight()
        => inHighlight = true;

    public override void ExitHightlight()
        => inHighlight = false;

    protected StatementCodeBlock CreateFunction()
        => new StatementCodeBlock(this, 0);

    protected static class LayerStack
    {
        public static void Process(CodeBlock block, int blockXOffset)
        {
            ListMultiValueDictionary<int, CodeBlock> xToBlocks = block.AllChildren.ToListMultiValueDictionary(child => child.LayerPos, child => child);

            xToBlocks.Add(block.LayerPos, block);

            foreach (var (_, list) in xToBlocks)
            {
                // sort in descending order - blocks start and zero and go down
                list.Sort((a, b) => b.StartZ.CompareTo(a.StartZ));

                List<int> positions = [];

                ListMultiValueDictionary<int, CodeBlock> layers = [];

                foreach (var item in list)
                {
                    if (item.BlockCount == 0)
                    {
                        continue;
                    }

                    int yPos = -1;

                    for (int j = 0; j < positions.Count; j++)
                    {
                        if (positions[j] > item.StartZ)
                        {
                            positions[j] = item.CurrentPos;
                            yPos = j;
                            break;
                        }
                    }

                    if (yPos == -1)
                    {
                        yPos = positions.Count;
                        positions.Add(item.CurrentPos);
                    }

                    layers.Add(yPos, item);
                }

                (int YPos, int Height)[] layerToInfo = new (int YPos, int Height)[layers.Count];

                foreach (var (layer, layerList) in layers)
                {
                    int maxHeight = layerList.Max(block => block.Height);

                    if (layer == 0)
                    {
                        layerToInfo[layer] = (0, maxHeight);
                    }
                    else
                    {
                        var (lastY, lastheight) = layerToInfo[layer - 1];
                        layerToInfo[layer] = (lastY + lastheight, maxHeight);
                    }
                }

                foreach (var (layer, layerList) in layers)
                {
                    int yPos = layerToInfo[layer].YPos;

                    foreach (CodeBlock cb in layerList)
                    {
                        foreach (var itemBlock in cb.Blocks)
                        {
                            itemBlock.Pos.X = blockXOffset * cb.LayerPos;
                            itemBlock.Pos.Y = yPos;
                        }
                    }
                }
            }
        }
    }

    protected abstract class CodeBlock
    {
        public readonly List<Block> Blocks = [];

        public readonly CodeBlock? Parent;

        public List<CodeBlock> Children = [];

        protected readonly GroundCodePlacer Placer;

        protected BlockDef? lastPlacedBlockType;

        public CodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
        {
            ArgumentNullException.ThrowIfNull(parent);
            Debug.Assert(layerOffset == 1 || layerOffset == -1, $"{nameof(layerOffset)} == 1 || {nameof(layerOffset)} == -1, Value: '{layerOffset}'");

            Placer = placer;
            StartPos = pos;
            CurrentPos = StartPos;
            HasParent = true;
            Parent = parent;
            LayerOffsetFromParent = layerOffset;
        }

        public CodeBlock(GroundCodePlacer placer, int pos)
        {
            Placer = placer;
            StartPos = pos;
            CurrentPos = StartPos;
            HasParent = false;
        }

        public int StartPos { get; private set; }

        public int CurrentPos { get; protected set; }

        public int Height { get; protected set; }

        /// <summary>
        /// Blocks of this <see cref="CodeBlock"/> and it's children
        /// </summary>
        public IEnumerable<Block> AllBlocks
            => Blocks
            .Concat(Children.SelectMany(child => child.AllBlocks));

        public int BlockCount => Blocks.Count;

        public IEnumerable<CodeBlock> AllChildren
            => Children
            .Concat(Children.SelectMany(child => child.AllChildren));

        [MemberNotNullWhen(true, nameof(Parent), nameof(LayerOffsetFromParent))]
        public bool HasParent { get; private set; }

        public int LayerPos => HasParent ? Parent.LayerPos + LayerOffsetFromParent.Value : 0;

        public int StartZ => StartPos + BlockZOffset;

        protected abstract int BlockZOffset { get; }

        protected int? LayerOffsetFromParent { get; private set; }

        public abstract StatementCodeBlock CreateStatementChild();

        public abstract ExpressionCodeBlock CreateExpressionChild();

        public Block PlaceBlock(BlockDef blockDef)
        {
            if (BlockCount != 0)
            {
                CurrentPos -= blockDef.Size.Z + BlockZOffset;
            }
            else
            {
                CurrentPos -= blockDef.Size.Z - 1;
            }

            Height = Math.Max(Height, blockDef.Size.Y);

            lastPlacedBlockType = blockDef;

            Block block = new Block(new Vector3I(0, 0, CurrentPos), blockDef);
            Blocks.Add(block);
            return block;
        }

        protected int CalculatePosOfNextChild()
            => CurrentPos + (lastPlacedBlockType is null ? 0 : lastPlacedBlockType.Size.Z - 1);

        protected void IncrementXOffsetOfStatementParent()
        {
            CodeBlock? current = this;

            while (current is not null)
            {
                if (current is StatementCodeBlock)
                {
                    current.LayerOffsetFromParent++;
                    return;
                }

                current = current.Parent;
            }
        }
    }

    protected class StatementCodeBlock : CodeBlock
    {
        public StatementCodeBlock(GroundCodePlacer placer, int pos)
            : base(placer, pos)
        {
        }

        public StatementCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
            : base(placer, pos, parent, layerOffset)
        {
        }

        protected override int BlockZOffset => 2;

        public override StatementCodeBlock CreateStatementChild()
        {
            StatementCodeBlock child = new StatementCodeBlock(Placer, CalculatePosOfNextChild(), this, 1);
            Children.Add(child);
            return child;
        }

        public override ExpressionCodeBlock CreateExpressionChild()
        {
            int lp = LayerPos;

            // is to the right of top statement
            if (lp > 0)
            {
                // if there is no space between this and the statement on the left, move self to the right
                if (Parent is not null && lp <= Parent.LayerPos + 1)
                {
                    IncrementXOffsetOfStatementParent();
                }
            }

            ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(), this, -1);
            Children.Add(child);
            return child;
        }
    }

    protected class ExpressionCodeBlock : CodeBlock
    {
        public ExpressionCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
            : base(placer, pos, parent, layerOffset)
        {
        }

        protected override int BlockZOffset => 0;

        public override StatementCodeBlock CreateStatementChild()
            => throw new InvalidOperationException();

        public override ExpressionCodeBlock CreateExpressionChild()
        {
            int lp = LayerPos;

            // is to the right of top statement
            if (lp > 0)
            {
                StatementCodeBlock? parent = GetStatementParentOfParent();

                // if there is no space between this and the statement on the left, move self to the right
                if (parent is not null && lp <= parent.LayerPos + 1)
                {
                    IncrementXOffsetOfStatementParent();
                }
            }

            ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(), this, -1);
            Children.Add(child);
            return child;

            // returns the statement to the left of this expression block
            StatementCodeBlock? GetStatementParentOfParent()
            {
                int statementParentCount = 0;
                CodeBlock? current = this;

                while (current is not null)
                {
                    current = current.Parent;
                    if (current is StatementCodeBlock sb)
                    {
                        // if 1 - the statement to the right
                        // if 2 - the statement to the left
                        if (++statementParentCount >= 2)
                        {
                            return sb;
                        }
                    }
                }

                return null;
            }
        }
    }
}
