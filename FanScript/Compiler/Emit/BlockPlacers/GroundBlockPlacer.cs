﻿using FanScript.Collections;
using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class GroundBlockPlacer : IBlockPlacer
    {
        const int assemblyXOffset = 5;
        const int blockXOffset = 3;

        public int CurrentCodeBlockBlocks => expressions.Count != 0 ? expressions.Peek().BlockCount : statements.Peek().BlockCount;

        protected readonly Stack<StatementBlock> statements = new();
        protected readonly Stack<ExpressionBlock> expressions = new();

        protected int highestX = 0;

        public Block Place(DefBlock defBlock)
        {
            Block block;

            if (expressions.Count != 0) block = expressions.Peek().PlaceBlock(defBlock);
            else block = statements.Peek().PlaceBlock(defBlock);

            int x = block.Pos.X + defBlock.Size.X - 1;
            if (x > highestX)
                highestX = x;

            return block;
        }

        public void EnterStatementBlock()
        {
            if (expressions.Count != 0) throw new Exception();

            if (statements.Count == 0) statements.Push(createAssembly());
            else statements.Push(statements.Peek().CreateChild());
        }
        public void ExitStatementBlock()
        {
            StatementBlock statement = statements.Pop();

            if (statements.Count != 0)
                statements.Peek().HandlePopChild(statement);
        }

        public void EnterExpression()
        {
            if (expressions.Count == 0) expressions.Push(statements.Peek().CreateExpression());
            else expressions.Push(expressions.Peek().CreateChild());
        }
        public void ExitExpression()
        {
            ExpressionBlock expression = expressions.Pop();

            if (expressions.Count != 0)
                expressions.Peek().HandlePopChild(expression);
            else
                statements.Peek().HandlePopChild(expression);
        }

        protected StatementBlock createAssembly()
        {
            highestX += assemblyXOffset;
            int x = highestX;
            highestX += blockXOffset;

            return new StatementBlock(new Vector3I(x, 0, 0), new TreeNode<List<int>>([int.MaxValue]));
        }

        protected abstract class CodeBlock
        {
            protected abstract int BlockZOffset { get; }

            public int BlockCount { get; protected set; }

            public readonly Vector3I StartPos;
            protected Vector3I blockPos;
            public Vector3I BlockPos => blockPos;

            public int MinX { get; protected set; }

            protected DefBlock? lastPlacedBlock;

            protected readonly List<Block> Blocks = new();

            protected TreeNode<List<int>> HeightNode;

            public CodeBlock(Vector3I _pos, TreeNode<List<int>> _heightNode)
            {
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

            public virtual Block PlaceBlock(DefBlock defBlock)
            {
                if (BlockCount != 0)
                    blockPos.Z -= defBlock.Size.Y + BlockZOffset;
                else
                    blockPos.Z -= defBlock.Size.Y - 1;

                lastPlacedBlock = defBlock;
                BlockCount++;

                Block block = new Block(blockPos, defBlock);
                Blocks.Add(block);
                return block;
            }

            protected virtual Vector3I nextChildPos(int childBranch, int xPos)
            {
                List<int> maxZs = HeightNode.GetOrCreateChild(childBranch, [int.MaxValue]).Value;

                int acceptableZ = blockPos.Z + BlockZOffset + (lastPlacedBlock is null ? 0 : lastPlacedBlock.Size.Y - 1);

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
            protected override int BlockZOffset => 2;

            public StatementBlock(Vector3I _pos, TreeNode<List<int>> _heightNode) : base(_pos, _heightNode)
            {
            }

            public override StatementBlock CreateChild()
            {
                return new StatementBlock(nextChildPos(1, blockPos.X + blockXOffset), HeightNode[1]);
            }

            public ExpressionBlock CreateExpression()
            {
                return new ExpressionBlock(nextChildPos(0, blockPos.X - blockXOffset), HeightNode[0]);
            }

            public override void HandlePopChild(CodeBlock child)
            {
                int appropriateX = StartPos.X + blockXOffset;

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
            protected override int BlockZOffset => 0;

            public ExpressionBlock(Vector3I _pos, TreeNode<List<int>> _heightNode) : base(_pos, _heightNode)
            {
            }

            public override ExpressionBlock CreateChild()
            {
                return new ExpressionBlock(nextChildPos(0, blockPos.X - blockXOffset), HeightNode[0]);
            }
        }
    }
}
