using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        private readonly Dictionary<int, PositionStack> availableLayerInfo = new();

        protected int highestX = 0;
        protected int highestXTotal = 0;
        protected int localLowestX = int.MaxValue;

        public Block Place(BlockDef blockDef)
        {
            Block block;

            if (expressions.Count != 0) block = expressions.Peek().PlaceBlock(blockDef);
            else block = statements.Peek().PlaceBlock(blockDef);

            int x = block.Pos.X + blockDef.Size.X - 1;
            if (x > highestX)
                highestX = x;
            if (x < localLowestX)
                localLowestX = x;

            return block;
        }

        public void EnterStatementBlock()
        {
            if (expressions.Count != 0) throw new Exception();

            if (statements.Count == 0) statements.Push(createFunction());
            else statements.Push(statements.Peek().CreateStatementChild());
        }
        public void ExitStatementBlock()
        {
            StatementBlock statement = statements.Pop();

            if (statements.Count != 0)
                statements.Peek().HandlePopChild(statement);
            else
            {
                availableLayerInfo.Clear();

                if (localLowestX < highestXTotal + functionXOffset)
                {
                    int move = (highestXTotal + functionXOffset) - localLowestX;
                    highestX += move;
                    statement.MoveX(move);
                }

                localLowestX = int.MaxValue;
                highestXTotal = highestX;
            }
        }

        public void EnterExpressionBlock()
        {
            if (expressions.Count == 0) expressions.Push(statements.Peek().CreateExpressionChild());
            else expressions.Push(expressions.Peek().CreateExpressionChild());
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
            highestXTotal += functionXOffset;

            return new StatementBlock(this, new Vector3I(highestXTotal, 0, 0));
        }

        protected abstract class CodeBlock
        {
            protected abstract int blockZOffset { get; }

            protected readonly GroundBlockPlacer Placer;

            public Vector3I StartPos { get; private set; }
            protected Vector3I blockPos;
            public Vector3I BlockPos => blockPos;

            /// <summary>
            /// Lowest position of a blocks placed by this <see cref="CodeBlock"/> or one of it's children
            /// </summary>
            public int MinX { get; protected set; }

            protected BlockDef? lastPlacedBlock;

            public int BlockCount => Blocks.Count;
            protected readonly List<Block> Blocks = new();
            internal readonly List<(CodeBlock, List<Block>)> ChildBlocks = new();

            [MemberNotNullWhen(true, nameof(Parent), nameof(offset))]
            public bool HasParent { get; private set; }
            public readonly CodeBlock? Parent;
            protected int? offset { get; private set; }
            public int LayerPos => HasParent ? Parent.LayerPos + offset.Value : 0;

            protected PositionStack.Request? YPosRequest;

            public CodeBlock(GroundBlockPlacer _placer, Vector3I _pos, CodeBlock _parent, int _offset)
            {
                ArgumentNullException.ThrowIfNull(_parent);
                Debug.Assert(_offset == 1 || _offset == -1, $"_offset == 1 || _offset == -1, Value: '{_offset}'");

                Placer = _placer;
                StartPos = _pos;
                blockPos = StartPos;
                HasParent = true;
                Parent = _parent;
                offset = _offset;

                MinX = StartPos.X;
            }
            public CodeBlock(GroundBlockPlacer _placer, Vector3I _pos)
            {
                Placer = _placer;
                StartPos = _pos;
                blockPos = StartPos;
                HasParent = false;

                MinX = StartPos.X;
            }

            public abstract StatementBlock CreateStatementChild();
            public abstract ExpressionBlock CreateExpressionChild();
            public virtual void RequestYPos()
            {
                Debug.Assert(YPosRequest is null);

                int acceptableZ = StartPos.Z + blockZOffset;// + (lastPlacedBlock is null ? 0 : lastPlacedBlock.Size.Y - 1);

                YPosRequest = Placer.availableLayerInfo.AddIfAbsent(LayerPos, new PositionStack()).RequestYPos(acceptableZ, blockPos.Z);
            }
            public virtual void AssignYPos()
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

            public virtual Block PlaceBlock(BlockDef blockDef)
            {
                if (BlockCount != 0)
                    blockPos.Z -= blockDef.Size.Y + blockZOffset;
                else
                    blockPos.Z -= blockDef.Size.Y - 1;

                lastPlacedBlock = blockDef;

                Block block = new Block(blockPos, blockDef);
                Blocks.Add(block);
                return block;
            }

            protected virtual Vector3I nextChildPos(int xPos, int blockZOffset)
                => new Vector3I(xPos, 0, blockPos.Z + (lastPlacedBlock is null ? 0 : lastPlacedBlock.Size.Y - 1));

            protected void incrementStatementOffset()
            {
                CodeBlock? current = this;

                while (current is not null)
                {
                    if (current is StatementBlock)
                    {
                        current.offset++;
                        return;
                    }

                    current = current.Parent;
                }
            }

            public void MoveX(int move)
            {
                for (int i = 0; i < Blocks.Count; i++)
                    Blocks[i].Pos.X += move;

                for (int blockIndex = 0; blockIndex < ChildBlocks.Count; blockIndex++)
                {
                    var (_, list) = ChildBlocks[blockIndex];

                    for (int i = 0; i < list.Count; i++)
                        list[i].Pos.X += move;
                }
            }
        }

        protected class StatementBlock : CodeBlock
        {
            protected override int blockZOffset => 2;

            public StatementBlock(GroundBlockPlacer _placer, Vector3I _pos)
                : base(_placer, _pos)
            {
            }
            public StatementBlock(GroundBlockPlacer _placer, Vector3I _pos, CodeBlock _parent, int _offset)
                : base(_placer, _pos, _parent, _offset)
            {
            }

            public override StatementBlock CreateStatementChild()
            {
                return new StatementBlock(Placer, nextChildPos(blockPos.X + Placer.BlockXOffset, blockZOffset), this, 1);
            }

            public override ExpressionBlock CreateExpressionChild()
            {
                int lp = LayerPos;
                // is to the right of top statement
                if (lp > 0)
                {
                    // if there is no space between this and the statement on the left, move self to the right
                    if (Parent is not null && lp <= Parent.LayerPos + 1)
                        incrementStatementOffset();
                }

                return new ExpressionBlock(Placer, nextChildPos(blockPos.X - Placer.BlockXOffset, ExpressionBlock.BlockZOffset), this, -1);
            }

            public override void HandlePopChild(CodeBlock child)
            {
                int appropriateX = StartPos.X + Placer.BlockXOffset;

                // if required, move the child to the right, necessary if it is statement and had expression children (to the left of it)
                if (child is StatementBlock statement && child.MinX < appropriateX)
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

        protected class ExpressionBlock : CodeBlock
        {
            public static readonly int BlockZOffset = 0;
            protected override int blockZOffset => BlockZOffset;

            public ExpressionBlock(GroundBlockPlacer _placer, Vector3I _pos, CodeBlock _parent, int _offset)
                : base(_placer, _pos, _parent, _offset)
            {
            }

            public override StatementBlock CreateStatementChild()
                => throw new InvalidOperationException();
            public override ExpressionBlock CreateExpressionChild()
            {
                int lp = LayerPos;
                // is to the right of top statement
                if (lp > 0)
                {
                    StatementBlock? parent = getStatementParentOfParent();
                    // if there is no space between this and the statement on the left, move self to the right
                    if (parent is not null && lp <= parent.LayerPos + 1)
                        incrementStatementOffset();
                }

                return new ExpressionBlock(Placer, nextChildPos(blockPos.X - Placer.BlockXOffset, blockZOffset), this, -1);

                // returns the statement to the left of this expression block
                StatementBlock? getStatementParentOfParent()
                {
                    int c = 0;
                    CodeBlock? current = this;

                    while (current is not null)
                    {
                        current = current.Parent;
                        if (current is StatementBlock sb)
                        {
                            if (++c >= 2)
                                return sb;
                        }
                    }

                    return null;
                }
            }
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        protected class PositionStack
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
