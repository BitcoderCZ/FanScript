using FanScript.Collections;
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
        const int childYOffset = 1;
        const int blockXOffset = 3;
        const int blockZOffset = 1;

        public Vector3I StartPos { get; set; } = new Vector3I(4, 0, 4);

        public int CurrentCodeBlockBlocks => expressions.Count != 0 ? expressions.Peek().BlockCount : statements.Peek().BlockCount;

        protected readonly Stack<StatementBlock> statements = new();
        protected readonly Stack<ExpressionBlock> expressions = new();

        protected int highestX = 0;

        public Vector3I Place(DefBlock defBlock)
        {
            Vector3I pos;

            if (expressions.Count != 0) pos = expressions.Peek().PlaceBlock(defBlock);
            else pos = statements.Peek().PlaceBlock(defBlock);

            int x = pos.X + defBlock.Size.X - 1;
            if (x > highestX)
                highestX = x;

            return pos;
        }

        public void EnterStatementBlock()
        {
            if (expressions.Count != 0) throw new Exception();

            if (statements.Count == 0) statements.Push(createAssembly());
            else statements.Push(statements.Peek().CreateChild());
        }
        public void ExitStatementBlock()
        {
            statements.Pop();
        }

        public void EnterExpression()
        {
            if (expressions.Count == 0) expressions.Push(statements.Peek().CreateExpression());
            else expressions.Push(expressions.Peek().CreateChild());
        }
        public void ExitExpression()
        {
            expressions.Pop();
        }

        protected StatementBlock createAssembly()
        {
            highestX += assemblyXOffset;
            int x = highestX;
            highestX += blockXOffset;

            return new StatementBlock(new Vector3I(x, 0, 0), new TreeNode<int>(0));
        }

        protected abstract class CodeBlock
        {
            public int BlockCount { get; protected set; }

            public readonly Vector3I StartPos;
            protected Vector3I blockPos;

            protected DefBlock? lastPlacedBlock;

            protected TreeNode<int> heightNode;

            public CodeBlock(Vector3I _pos, TreeNode<int> _heightNode)
            {
                StartPos = _pos;
                blockPos = StartPos;
                heightNode = _heightNode;
            }

            public abstract CodeBlock CreateChild();

            public virtual Vector3I PlaceBlock(DefBlock defBlock)
            {
                if (BlockCount != 0)
                    blockPos.Z -= defBlock.Size.Y + blockZOffset;

                lastPlacedBlock = defBlock;
                BlockCount++;
                return blockPos;
            }
        }

        protected class StatementBlock : CodeBlock
        {
            public StatementBlock(Vector3I _pos, TreeNode<int> _heightNode) : base(_pos, _heightNode)
            {
            }

            public override StatementBlock CreateChild()
            {
                var childNode = heightNode.GetOrCreateChild(1, 0);
                int yLevel = childNode.Value;
                childNode.Value += childYOffset;
                return new StatementBlock(new Vector3I(blockPos.X + blockXOffset * 3, yLevel, blockPos.Z), childNode);
            }

            public ExpressionBlock CreateExpression()
            {
                var childNode = heightNode.GetOrCreateChild(0, 0);
                int yLevel = childNode.Value;
                childNode.Value += childYOffset;
                return new ExpressionBlock(new Vector3I(blockPos.X - blockXOffset, yLevel, blockPos.Z), childNode);
            }
        }

        protected class ExpressionBlock : CodeBlock
        {
            public ExpressionBlock(Vector3I _pos, TreeNode<int> _heightNode) : base(_pos, _heightNode)
            {
            }

            public override ExpressionBlock CreateChild()
            {
                var childNode = heightNode.GetOrCreateChild(0, 0);
                int yLevel = childNode.Value;
                childNode.Value += childYOffset;
                return new ExpressionBlock(new Vector3I(blockPos.X - blockXOffset, yLevel, blockPos.Z), childNode);
            }
        }
    }
}
