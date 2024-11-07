using System.Collections.Immutable;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit.BlockBuilders
{
    public abstract class BlockBuilder
    {
        protected List<BlockSegment> segments = [];
        protected List<Block> highlightedBlocks = [];
        protected List<ConnectionRecord> connections = [];
        protected List<ValueRecord> values = [];

        public interface IArgs
        {
        }

        public virtual void AddBlockSegments(IEnumerable<Block> blocks)
        {
            BlockSegment segment = new BlockSegment(blocks);

            segments.Add(segment);
        }

        public virtual void AddHighlightedBlock(Block block)
            => highlightedBlocks.Add(block);

        public virtual void Connect(IConnectTarget from, IConnectTarget to)
            => connections.Add(new ConnectionRecord(from, to));

        public virtual void SetBlockValue(Block block, int valueIndex, object value)
            => values.Add(new ValueRecord(block, valueIndex, value));

        public abstract object Build(Vector3I posToBuildAt, IArgs? args = null);

        public virtual void Clear()
        {
            segments.Clear();
            connections.Clear();
            values.Clear();
        }

        internal void Connect(IEmitStore? from, IEmitStore to)
        {
            if (from is NopEmitStore || to is NopEmitStore)
            {
                return;
            }

            if (to.In is null)
            {
                return;
            }

            if (from?.Out is not null)
            {
                foreach (var target in from.Out)
                {
                    Connect(target, to.In);
                }
            }
        }

        protected Block[] PreBuild(Vector3I posToBuildAt, bool sortByPos)
        {
            if (posToBuildAt.X < 0 || posToBuildAt.Y < 0 || posToBuildAt.Z < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(posToBuildAt), $"{nameof(posToBuildAt)} must be >= 0");
            }
            else if (segments.Count == 0)
            {
                return [];
            }

            int totalBlockCount = highlightedBlocks.Count;
            Vector3I[] segmentSizes = new Vector3I[segments.Count];

            for (int i = 0; i < segments.Count; i++)
            {
                totalBlockCount += segments[i].Blocks.Length;
                segmentSizes[i] = segments[i].Size + new Vector3I(2, 1, 2); // margin
            }

            Vector3I[] segmentPositions = BinPacker.Compute(segmentSizes);

            Block[] blocks = new Block[totalBlockCount];

            Vector3I highlightedPos = posToBuildAt;
            for (int i = 0; i < highlightedBlocks.Count; i++)
            {
                highlightedBlocks[i].Pos = highlightedPos;
                highlightedPos.X += 3;
            }

            highlightedBlocks.CopyTo(blocks);

            int index = highlightedBlocks.Count;
            Vector3I off = highlightedBlocks.Count > 0 ? new Vector3I(0, 0, 4) : Vector3I.Zero;

            for (int i = 0; i < segments.Count; i++)
            {
                BlockSegment segment = segments[i];

                segment.Move((segmentPositions[i] + posToBuildAt + off) - segment.MinPos);

                segment.Blocks.CopyTo(blocks, index);
                index += segment.Blocks.Length;
            }

            if (sortByPos)
            {
                Array.Sort(blocks, (a, b) =>
                {
                    int comp = a.Pos.Z.CompareTo(b.Pos.Z);
                    return comp == 0 ? a.Pos.X.CompareTo(b.Pos.X) : comp;
                });
            }

            return blocks;
        }

        protected virtual Vector3I ChooseSubPos(Vector3I pos)
            => new Vector3I(7, 3, 3);

        protected readonly record struct ConnectionRecord(IConnectTarget From, IConnectTarget To)
        {
        }

        protected readonly record struct ValueRecord(Block Block, int ValueIndex, object Value)
        {
        }

        protected class BlockSegment
        {
            public readonly ImmutableArray<Block> Blocks;

            public BlockSegment(IEnumerable<Block> blocks)
            {
                ArgumentNullException.ThrowIfNull(blocks);

                Blocks = blocks.ToImmutableArray();
                if (Blocks.Length == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(blocks), $"{nameof(blocks)} cannot be empty.");
                }

                CalculateMinMax();
            }

            public Vector3I MinPos { get; private set; }

            public Vector3I MaxPos { get; private set; }

            public Vector3I Size => (MaxPos - MinPos) + Vector3I.One;

            public void Move(Vector3I move)
            {
                if (move == Vector3I.Zero)
                {
                    return;
                }

                for (int i = 0; i < Blocks.Length; i++)
                {
                    Blocks[i].Pos += move;
                }

                MinPos += move;
                MaxPos += move;
            }

            private void CalculateMinMax()
            {
                Vector3I min = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
                Vector3I max = new Vector3I(int.MinValue, int.MinValue, int.MinValue);

                for (int i = 0; i < Blocks.Length; i++)
                {
                    BlockDef type = Blocks[i].Type;

                    min = Vector3I.Min(Blocks[i].Pos, min);
                    max = Vector3I.Max(Blocks[i].Pos + type.Size, max);
                }

                MinPos = min;
                MaxPos = max - Vector3I.One;
            }
        }
    }
}
