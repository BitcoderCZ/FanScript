using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Emit.BlockPlacers
{
    public class GroundCodePlacer : CodePlacer
    {
        public override int CurrentCodeBlockBlocks => expressions.Count != 0 ? expressions.Peek().BlockCount : statements.Peek().BlockCount;

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
        private int blockXOffset = 3;

        protected readonly Stack<StatementCodeBlock> statements = new();
        protected readonly Stack<ExpressionCodeBlock> expressions = new();

        protected bool inHighlight = false;
        protected int highlightX = 0;

        public GroundCodePlacer(BlockBuilder builder)
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
                if (expressions.Count != 0) block = expressions.Peek().PlaceBlock(blockDef);
                else block = statements.Peek().PlaceBlock(blockDef);
            }

            return block;
        }

        public override void EnterStatementBlock()
        {
            Debug.Assert(expressions.Count == 0, "Cannot enter a statement block while being in an expression block");

            if (statements.Count == 0) statements.Push(createFunction());
            else statements.Push(statements.Peek().CreateStatementChild());
        }
        public override void ExitStatementBlock()
        {
            Debug.Assert(expressions.Count == 0, "Cannot exit a statement block while being in an expression block");

            StatementCodeBlock statement = statements.Pop();

            if (statements.Count == 1)
            {
                if (statement.Parent!.Parent is not null)
                    throw new Exception("Parent not null.");

                // if this is child of the top statement, process and assign x and y position to blocks
                LayerStack.Process(statement, BlockXOffset);
            }
            else if (statements.Count == 0)
            {
                // end of function
                Builder.AddBlockSegments(statement.AllBlocks);
            }
        }

        public override void EnterExpressionBlock()
        {
            Debug.Assert(statements.Count > 0, "Cannot enter an expression block without being in a statement block");

            if (expressions.Count == 0) expressions.Push(statements.Peek().CreateExpressionChild());
            else expressions.Push(expressions.Peek().CreateExpressionChild());
        }
        public override void ExitExpressionBlock()
        {
            Debug.Assert(statements.Count > 0, "Cannot exit an expression block without being in a statement block");

            ExpressionCodeBlock expression = expressions.Pop();

            if (statements.Count == 1)
            {
                if (expression.Parent!.Parent is not null)
                    throw new Exception("Parent not null.");
                // if this is child of the top statement, process and assign x and y position to blocks
                LayerStack.Process(expression, BlockXOffset);
            }
        }

        protected StatementCodeBlock createFunction()
        {
            return new StatementCodeBlock(this, 0);
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

        protected abstract class CodeBlock
        {
            protected abstract int blockZOffset { get; }

            protected readonly GroundCodePlacer Placer;

            public int StartPos { get; private set; }
            public int CurrentPos { get; protected set; }

            protected BlockDef? LastPlacedBlockType;

            /// <summary>
            /// Blocks of this <see cref="CodeBlock"/> and it's children
            /// </summary>
            public IEnumerable<Block> AllBlocks
                => Blocks
                .Concat(Children.SelectMany(child => child.AllBlocks));
            public int BlockCount => Blocks.Count;
            public readonly List<Block> Blocks = new();
            public IEnumerable<CodeBlock> AllChildren
                => Children
                .Concat(Children.SelectMany(child => child.AllChildren));
            public List<CodeBlock> Children = new List<CodeBlock>();

            [MemberNotNullWhen(true, nameof(Parent), nameof(LayerOffsetFromParent))]
            public bool HasParent { get; private set; }
            public readonly CodeBlock? Parent;
            protected int? LayerOffsetFromParent { get; private set; }

            public int LayerPos => HasParent ? Parent.LayerPos + LayerOffsetFromParent.Value : 0;
            public int StartZ => StartPos + blockZOffset;

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

            public abstract StatementCodeBlock CreateStatementChild();
            public abstract ExpressionCodeBlock CreateExpressionChild();

            public Block PlaceBlock(BlockDef blockDef)
            {
                if (BlockCount != 0)
                    CurrentPos -= blockDef.Size.Y + blockZOffset;
                else
                    CurrentPos -= blockDef.Size.Y - 1;

                LastPlacedBlockType = blockDef;

                Block block = new Block(new Vector3I(0, 0, CurrentPos), blockDef);
                Blocks.Add(block);
                return block;
            }

            protected int CalculatePosOfNextChild(int blockZOffset)
                => CurrentPos + (LastPlacedBlockType is null ? 0 : LastPlacedBlockType.Size.Y - 1);

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
            protected override int blockZOffset => 2;

            public StatementCodeBlock(GroundCodePlacer placer, int pos)
                : base(placer, pos)
            {
            }
            public StatementCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
                : base(placer, pos, parent, layerOffset)
            {
            }

            public override StatementCodeBlock CreateStatementChild()
            {
                StatementCodeBlock child = new StatementCodeBlock(Placer, CalculatePosOfNextChild(blockZOffset), this, 1);
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
                        IncrementXOffsetOfStatementParent();
                }

                ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(ExpressionCodeBlock.BlockZOffset), this, -1);
                Children.Add(child);
                return child;
            }
        }

        protected class ExpressionCodeBlock : CodeBlock
        {
            public static readonly int BlockZOffset = 0;
            protected override int blockZOffset => BlockZOffset;

            public ExpressionCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
                : base(placer, pos, parent, layerOffset)
            {
            }

            public override StatementCodeBlock CreateStatementChild()
                => throw new InvalidOperationException();
            public override ExpressionCodeBlock CreateExpressionChild()
            {
                int lp = LayerPos;
                // is to the right of top statement
                if (lp > 0)
                {
                    StatementCodeBlock? parent = getStatementParentOfParent();
                    // if there is no space between this and the statement on the left, move self to the right
                    if (parent is not null && lp <= parent.LayerPos + 1)
                        IncrementXOffsetOfStatementParent();
                }

                ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(blockZOffset), this, -1);
                Children.Add(child);
                return child;

                // returns the statement to the left of this expression block
                StatementCodeBlock? getStatementParentOfParent()
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
                                return sb;
                        }
                    }

                    return null;
                }
            }
        }

        protected static class LayerStack
        {
            public static void Process(CodeBlock block, int blockXOffset)
            {
                MultiValueDictionary<int, CodeBlock> xToBlocks = block.AllChildren.ToMultiValueDictionary(child => child.LayerPos, child => child);

                xToBlocks.Add(block.LayerPos, block);

                foreach (var (_, list) in xToBlocks)
                {
                    // sort in descending order - blocks start and zero and go down
                    list.Sort((a, b) => b.StartPos.CompareTo(a.StartPos));

                    List<int> positions = new();

                    foreach (var item in list)
                    {
                        if (item.BlockCount == 0)
                            continue;

                        int yPos = -1;

                        for (int j = 0; j < positions.Count; j++)
                            if (positions[j] > item.StartZ)
                            {
                                positions[j] = item.CurrentPos;
                                yPos = j;
                                break;
                            }

                        if (yPos == -1)
                        {
                            yPos = positions.Count;
                            positions.Add(item.CurrentPos);
                        }

                        foreach (var itemBlock in item.Blocks)
                        {
                            itemBlock.Pos.X = blockXOffset * item.LayerPos;
                            itemBlock.Pos.Y = yPos;
                        }
                    }
                }
            }
        }
    }
}
