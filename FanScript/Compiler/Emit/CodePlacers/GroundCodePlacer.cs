﻿using FanScript.FCInfo;
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

        private readonly Dictionary<int, LayerStack> availableLayerInfo = new();

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

            if (statements.Count != 0)
                statements.Peek().HandlePopChild(statement);
            else
            {
                // end of function
                availableLayerInfo.Clear();

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

            if (expressions.Count != 0)
                expressions.Peek().HandlePopChild(expression);
            else
                statements.Peek().HandlePopChild(expression);
        }

        protected StatementCodeBlock createFunction()
        {
            return new StatementCodeBlock(this, new Vector3I(0, 0, 0));
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

            public Vector3I StartPos { get; private set; }
            public Vector3I BlockPos { get; protected set; }

            /// <summary>
            /// Lowest position of a blocks placed by this <see cref="CodeBlock"/> or one of it's children
            /// </summary>
            public int MinX { get; protected set; }

            protected BlockDef? LastPlacedBlockType;

            /// <summary>
            /// Blocks of this <see cref="CodeBlock"/> and it's children
            /// </summary>
            public IEnumerable<Block> AllBlocks => Blocks
                .Concat(ChildBlocks.SelectMany(item => item.Item2));
            public int BlockCount => Blocks.Count;
            protected readonly List<Block> Blocks = new();
            internal readonly List<(CodeBlock, List<Block>)> ChildBlocks = new();

            [MemberNotNullWhen(true, nameof(Parent), nameof(XOffsetFromParent))]
            public bool HasParent { get; private set; }
            public readonly CodeBlock? Parent;
            protected int? XOffsetFromParent { get; private set; }
            public int LayerPos => HasParent ? Parent.LayerPos + XOffsetFromParent.Value : 0;

            protected LayerStack.Request? YPosRequest;

            public CodeBlock(GroundCodePlacer placer, Vector3I pos, CodeBlock parent, int xOffsetFromParent)
            {
                ArgumentNullException.ThrowIfNull(parent);
                Debug.Assert(xOffsetFromParent == 1 || xOffsetFromParent == -1, $"{nameof(xOffsetFromParent)} == 1 || {nameof(xOffsetFromParent)} == -1, Value: '{xOffsetFromParent}'");

                Placer = placer;
                StartPos = pos;
                BlockPos = StartPos;
                HasParent = true;
                Parent = parent;
                XOffsetFromParent = xOffsetFromParent;

                MinX = StartPos.X;
            }
            public CodeBlock(GroundCodePlacer placer, Vector3I pos)
            {
                Placer = placer;
                StartPos = pos;
                BlockPos = StartPos;
                HasParent = false;

                MinX = StartPos.X;
            }

            public abstract StatementCodeBlock CreateStatementChild();
            public abstract ExpressionCodeBlock CreateExpressionChild();
            public void RequestYPos()
            {
                Debug.Assert(YPosRequest is null);

                int acceptableZ = StartPos.Z + blockZOffset;

                YPosRequest = Placer.availableLayerInfo.AddIfAbsent(LayerPos, new LayerStack()).RequestYPos(acceptableZ, BlockPos.Z);
            }
            public void AssignYPos()
            {
                if (YPosRequest is null || YPosRequest.Result is null)
                    return;

                int yPos = YPosRequest.Result.Value;

                for (int i = 0; i < Blocks.Count; i++)
                    Blocks[i].Pos.Y = yPos;
            }
            public virtual void HandlePopChild(CodeBlock child)
            {
                if (child.MinX < MinX)
                    MinX = child.MinX;

                ChildBlocks.Add((child, child.Blocks));
                ChildBlocks.AddRange(child.ChildBlocks);
            }

            public Block PlaceBlock(BlockDef blockDef)
            {
                if (BlockCount != 0)
                    BlockPos = BlockPos with { Z = BlockPos.Z - (blockDef.Size.Y + blockZOffset) };
                else
                    BlockPos = BlockPos with { Z = BlockPos.Z - (blockDef.Size.Y - 1) };

                LastPlacedBlockType = blockDef;

                Block block = new Block(BlockPos, blockDef);
                Blocks.Add(block);
                return block;
            }

            protected Vector3I CalculatePosOfNextChild(int xPos, int blockZOffset)
                => new Vector3I(xPos, 0, BlockPos.Z + (LastPlacedBlockType is null ? 0 : LastPlacedBlockType.Size.Y - 1));

            protected void IncrementXOffsetOfStatementParent()
            {
                CodeBlock? current = this;

                while (current is not null)
                {
                    if (current is StatementCodeBlock)
                    {
                        current.XOffsetFromParent++;
                        return;
                    }

                    current = current.Parent;
                }
            }
        }

        protected class StatementCodeBlock : CodeBlock
        {
            protected override int blockZOffset => 2;

            public StatementCodeBlock(GroundCodePlacer placer, Vector3I pos)
                : base(placer, pos)
            {
            }
            public StatementCodeBlock(GroundCodePlacer placer, Vector3I pos, CodeBlock parent, int offset)
                : base(placer, pos, parent, offset)
            {
            }

            public override StatementCodeBlock CreateStatementChild()
            {
                return new StatementCodeBlock(Placer, CalculatePosOfNextChild(BlockPos.X + Placer.BlockXOffset, blockZOffset), this, 1);
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

                return new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(BlockPos.X - Placer.BlockXOffset, ExpressionCodeBlock.BlockZOffset), this, -1);
            }

            public override void HandlePopChild(CodeBlock child)
            {
                int appropriateX = StartPos.X + Placer.BlockXOffset;

                // if required, move the child to the right, necessary if it is statement and had expression children (to the left of it)
                if (child is StatementCodeBlock statement && child.MinX < appropriateX)
                {
                    int move = appropriateX - child.MinX;

                    foreach (Block block in statement.ChildBlocks
                        .SelectMany(item => item.Item2)
                        .Concat(statement.Blocks))
                        block.Pos.X += move;
                }

                base.HandlePopChild(child);

                // if this is the top statement, process and assign y position to blocks
                if (Parent is null)
                {
                    child.RequestYPos();
                    foreach (var _child in child.ChildBlocks.Select(item => item.Item1))
                        _child.RequestYPos();

                    foreach (var info in Placer.availableLayerInfo)
                        info.Value.ProcessRequests();

                    child.AssignYPos();
                    foreach (var _child in child.ChildBlocks.Select(item => item.Item1))
                        _child.AssignYPos();
                }
            }
        }

        protected class ExpressionCodeBlock : CodeBlock
        {
            public static readonly int BlockZOffset = 0;
            protected override int blockZOffset => BlockZOffset;

            public ExpressionCodeBlock(GroundCodePlacer placer, Vector3I pos, CodeBlock parent, int offset)
                : base(placer, pos, parent, offset)
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

                return new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(BlockPos.X - Placer.BlockXOffset, blockZOffset), this, -1);

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

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        protected class LayerStack
        {
            private List<int> positions = new();

            private readonly List<Request> requests = new();

            public void ProcessRequests()
            {
                if (requests.Count == 0)
                    return;

                // sort in descending order
                requests.Sort((a, b) => b.PlaceZ.CompareTo(a.PlaceZ));

                for (int i = 0; i < requests.Count; i++)
                {
                    Request request = requests[i];

                    int yPos = -1;
                    for (int j = 0; j < positions.Count; j++)
                        if (positions[j] > request.AcceptableZ)
                        {
                            positions[j] = request.PlaceZ;
                            yPos = j;
                            break;
                        }

                    if (yPos == -1)
                    {
                        positions.Add(request.PlaceZ);
                        yPos = positions.Count - 1;
                    }

                    request.Result = yPos;
                }

                requests.Clear();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="acceptableZ">Z position that should be free</param>
            /// <param name="placeZ">Z position that will get written</param>
            /// <returns>The <see cref="Request"/></returns>
            public Request RequestYPos(int acceptableZ, int placeZ)
            {
                Request request = new Request(acceptableZ, placeZ);
                requests.Add(request);
                return request;
            }

            private string DebuggerDisplay => $"[{string.Join(", ", positions)}]";

            public class Request
            {
                public readonly int AcceptableZ;
                public readonly int PlaceZ;

                public int? Result { get; internal set; }

                internal Request(int acceptableZ, int placeZ)
                {
                    AcceptableZ = acceptableZ;
                    PlaceZ = placeZ;
                }
            }
        }
    }
}
