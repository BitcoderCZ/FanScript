﻿using FanScript.Collections;
using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class GroundBlockPlacer : IBlockPlacer
    {
        const int functionXOffset = 5;

        public int CurrentCodeBlockBlocks => expressions.Count != 0 ? expressions.Peek().BlockCount : statements.Peek().BlockCount;

        private int blockXOffset = 3;
        /// <summary>
        /// Horizontal space between blocks of code (min value and default is <see langword="3"/>)
        /// </summary>
        public int BlockXOffset
        {
            get => blockXOffset;
            init
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 3, nameof(BlockXOffset));
                blockXOffset = value;
            }
        }

        protected readonly Stack<StatementBlock> statements = new();
        protected readonly Stack<ExpressionBlock> expressions = new();

        protected int highestX = 0;

        public Block Place(BlockDef blockDef)
        {
            Block block;

            if (expressions.Count != 0) block = expressions.Peek().PlaceBlock(blockDef);
            else block = statements.Peek().PlaceBlock(blockDef);

            int x = block.Pos.X + blockDef.Size.X - 1;
            if (x > highestX)
                highestX = x;

            return block;
        }

        public void EnterStatementBlock()
        {
            if (expressions.Count != 0) throw new Exception();

            if (statements.Count == 0) statements.Push(createFunction());
            else statements.Push(statements.Peek().CreateChild());
        }
        public void ExitStatementBlock()
        {
            StatementBlock statement = statements.Pop();

            if (statements.Count != 0)
                statements.Peek().HandlePopChild(statement);
        }

        public void EnterExpressionBlock()
        {
            if (expressions.Count == 0) expressions.Push(statements.Peek().CreateExpression());
            else expressions.Push(expressions.Peek().CreateChild());
        }
        public void ExitExpressionBlock()
        {
            ExpressionBlock expression = expressions.Pop();

            if (expressions.Count != 0)
                expressions.Peek().HandlePopChild(expression);
            else
                statements.Peek().HandlePopChild(expression);
        }

        protected StatementBlock createFunction()
        {
            highestX += functionXOffset;
            int x = highestX;
            highestX += blockXOffset;

            return new StatementBlock(this, new Vector3I(x, 0, 0), new TreeNode<List<int>>([int.MaxValue]));
        }

        protected abstract class CodeBlock
        {
            protected abstract int blockZOffset { get; }

            public int BlockCount { get; protected set; }

            protected readonly GroundBlockPlacer Placer;

            public readonly Vector3I StartPos;
            protected Vector3I blockPos;
            public Vector3I BlockPos => blockPos;

            public int MinX { get; protected set; }

            protected BlockDef? lastPlacedBlock;

            protected readonly List<Block> Blocks = new();

            protected TreeNode<List<int>> HeightNode;

            public CodeBlock(GroundBlockPlacer _placer, Vector3I _pos, TreeNode<List<int>> _heightNode)
            {
                Placer = _placer;
                StartPos = _pos;
                blockPos = StartPos;
                HeightNode = _heightNode;

                MinX = StartPos.X;
            }

            public abstract CodeBlock CreateChild();
            public virtual void HandlePopChild(CodeBlock child)
            {
                if (child.MinX < MinX)
                    MinX = child.MinX;

                int childBranch = child switch
                {
                    ExpressionBlock => 0,
                    StatementBlock => 1,
                    _ => throw new InvalidDataException($"Unknown CodeBlock type '{child.GetType().FullName}'"),
                };

                HeightNode[childBranch].Value[child.StartPos.Y] = Math.Min(child.BlockPos.Z, HeightNode[childBranch].Value[child.StartPos.Y]); // the min shouldn't be necessary, but I'll do it just in case

                Blocks.AddRange(child.Blocks);
            }

            public virtual Block PlaceBlock(BlockDef blockDef)
            {
                if (BlockCount != 0)
                    blockPos.Z -= blockDef.Size.Y + blockZOffset;
                else
                    blockPos.Z -= blockDef.Size.Y - 1;

                lastPlacedBlock = blockDef;
                BlockCount++;

                Block block = new Block(blockPos, blockDef);
                Blocks.Add(block);
                return block;
            }

            protected virtual Vector3I nextChildPos(int childBranch, int xPos, int blockZOffset)
            {
                List<int> maxZs = HeightNode.GetOrCreateChild(childBranch, [int.MaxValue]).Value;

                int acceptableZ = blockPos.Z + blockZOffset + (lastPlacedBlock is null ? 0 : lastPlacedBlock.Size.Y - 1);

                for (int i = 0; i < maxZs.Count; i++)
                    if (maxZs[i] > acceptableZ)
                        return createPos(i);

                maxZs.Add(blockPos.Z);

                return createPos(maxZs.Count - 1);

                Vector3I createPos(int y)
                    => new Vector3I(xPos, y, blockPos.Z + (lastPlacedBlock is null ? 0 : lastPlacedBlock.Size.Y - 1));
            }
        }

        protected class StatementBlock : CodeBlock
        {
            protected override int blockZOffset => 2;

            public StatementBlock(GroundBlockPlacer _placer, Vector3I _pos, TreeNode<List<int>> _heightNode) : base(_placer, _pos, _heightNode)
            {
            }

            public override StatementBlock CreateChild()
            {
                return new StatementBlock(Placer, nextChildPos(1, blockPos.X + Placer.BlockXOffset, blockZOffset), HeightNode[1]);
            }

            public ExpressionBlock CreateExpression()
            {
                return new ExpressionBlock(Placer, nextChildPos(0, blockPos.X - Placer.BlockXOffset, ExpressionBlock.BlockZOffset), HeightNode[0]);
            }

            public override void HandlePopChild(CodeBlock child)
            {
                int appropriateX = StartPos.X + Placer.BlockXOffset;

                if (child is StatementBlock statement && child.MinX < appropriateX)
                {
                    int move = appropriateX - child.MinX;

                    for (int i = 0; i < statement.Blocks.Count; i++)
                        statement.Blocks[i].Pos.X += move;
                }

                base.HandlePopChild(child);
            }
        }

        protected class ExpressionBlock : CodeBlock
        {
            public static readonly int BlockZOffset = 0;
            protected override int blockZOffset => BlockZOffset;

            public ExpressionBlock(GroundBlockPlacer _placer, Vector3I _pos, TreeNode<List<int>> _heightNode) : base(_placer, _pos, _heightNode)
            {
            }

            public override ExpressionBlock CreateChild()
            {
                return new ExpressionBlock(Placer, nextChildPos(0, blockPos.X - Placer.BlockXOffset, BlockZOffset), HeightNode[0]);
            }
        }
    }
}
