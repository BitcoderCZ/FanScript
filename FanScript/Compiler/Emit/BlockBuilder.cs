using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FanScript.Compiler.Emit
{
    public abstract class BlockBuilder
    {
        public abstract BuildPlatformInfo PlatformInfo { get; }

        protected List<BlockSegment> segments = new();
        protected List<ConnectionRecord> connections = new();
        protected List<ValueRecord> values = new();

        public virtual void AddBlockSegments(IEnumerable<Block> blocks)
        {
            BlockSegment segment = new BlockSegment(blocks);

            segments.Add(segment);
        }

        internal void Connect(EmitStore? from, EmitStore to)
        {
            if (from is NopEmitStore || to is NopEmitStore)
                return;

            if (to.In is null)
                return;

            if (from?.Out is not null)
                foreach (var target in from.Out)
                    Connect(target, to.In);
        }
        public virtual void Connect(ConnectTarget from, ConnectTarget to)
            => connections.Add(new ConnectionRecord(from, to));

        public virtual void SetBlockValue(Block block, int valueIndex, object value)
            => values.Add(new ValueRecord(block, valueIndex, value));

        public abstract object Build(Vector3I posToBuildAt, params object[] args);

        protected Block[] PreBuild(Vector3I posToBuildAt, bool sortByPos)
        {
            if (posToBuildAt.X < 0 || posToBuildAt.Y < 0 || posToBuildAt.Z < 0)
                throw new ArgumentOutOfRangeException(nameof(posToBuildAt), $"{nameof(posToBuildAt)} must be >= 0");
            else if (segments.Count == 0)
                return Array.Empty<Block>();

            int totalBlockCount = 0;
            Vector3I[] segmentSizes = new Vector3I[segments.Count];

            for (int i = 0; i < segments.Count; i++)
            {
                totalBlockCount += segments[i].Blocks.Length;
                segmentSizes[i] = segments[i].Size + new Vector3I(2, 0, 2); // margin
            }

            Vector3I[] segmentPositions = BinPacker.Compute(segmentSizes);

            Block[] blocks = new Block[totalBlockCount];

            int index = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                BlockSegment segment = segments[i];

                segment.Move((segmentPositions[i] + posToBuildAt) - segment.MinPos);

                segment.Blocks.CopyTo(blocks, index);
                index += segment.Blocks.Length;
            }

            if (sortByPos)
            {
                Array.Sort(blocks, (a, b) =>
                {
                    int comp = a.Pos.Z.CompareTo(b.Pos.Z);
                    if (comp == 0)
                        return a.Pos.X.CompareTo(b.Pos.X);
                    else
                        return comp;
                });
            }

            return blocks;
        }

        protected virtual Vector3I ChooseSubPos(Vector3I pos)
            => new Vector3I(7, 3, 3);

        public virtual void Clear()
        {
            segments.Clear();
            connections.Clear();
            values.Clear();
        }

        protected readonly record struct ConnectionRecord(ConnectTarget From, ConnectTarget To)
        {
        }

        protected readonly record struct ValueRecord(Block Block, int ValueIndex, object Value)
        {
        }

        protected class BlockSegment
        {
            public readonly ImmutableArray<Block> Blocks;

            public Vector3I MinPos { get; private set; }
            public Vector3I MaxPos { get; private set; }

            public Vector3I Size => (MaxPos - MinPos) + Vector3I.One;

            public BlockSegment(IEnumerable<Block> blocks)
            {
                ArgumentNullException.ThrowIfNull(blocks);

                Blocks = blocks.ToImmutableArray();
                if (Blocks.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(blocks), $"{nameof(blocks)} cannot be empty.");

                calculateMinMax();
            }

            public void Move(Vector3I move)
            {
                if (move == Vector3I.Zero)
                    return;

                for (int i = 0; i < Blocks.Length; i++)
                    Blocks[i].Pos += move;

                MinPos += move;
                MaxPos += move;
            }

            private void calculateMinMax()
            {
                Vector3I min = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
                Vector3I max = new Vector3I(int.MinValue, int.MinValue, int.MinValue);

                for (int i = 0; i < Blocks.Length; i++)
                {
                    BlockDef type = Blocks[i].Type;

                    min = Vector3I.Min(Blocks[i].Pos, min);
                    max = Vector3I.Max(Blocks[i].Pos + new Vector3I(type.Size.X, 1, type.Size.Y), max);
                }

                MinPos = min;
                MaxPos = max - Vector3I.One;
            }
        }
    }
}
