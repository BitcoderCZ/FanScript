using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit
{
    public abstract class CodeBuilder
    {
        public abstract BuildPlatformInfo PlatformInfo { get; }

        public IBlockPlacer BlockPlacer { get; protected set; }

        protected List<Block> blocks = new();
        protected List<ConnectionRecord> connections = new();
        protected List<ValueRecord> values = new();

        public CodeBuilder(IBlockPlacer blockPlacer)
        {
            BlockPlacer = blockPlacer;
        }

        public virtual Block AddBlock(BlockDef blockDef)
        {
            Block block = BlockPlacer.Place(blockDef);

            blocks.Add(block);

            return block;
        }

        internal void Connect(EmitStore? from, EmitStore to)
        {
            if (from is NopEmitStore || to is NopEmitStore)
            {
                Console.WriteLine("Tried To connect nop store");
                return;
            }

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

        public abstract object Build(Vector3I startPos, params object[] args);

        protected void PreBuild(Vector3I startPos)
        {
            if (startPos.X < 0 || startPos.Y < 0 || startPos.Z < 0)
                throw new ArgumentOutOfRangeException(nameof(startPos), $"{nameof(startPos)} must be >= 0");
            else if (blocks.Count == 0)
                return;

            Vector3I lowestPos = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            for (int i = 0; i < blocks.Count; i++)
            {
                Vector3I pos = blocks[i].Pos;

                if (pos.X < lowestPos.X)
                    lowestPos.X = pos.X;
                if (pos.Y < lowestPos.Y)
                    lowestPos.Y = pos.Y;
                if (pos.Z < lowestPos.Z)
                    lowestPos.Z = pos.Z;
            }

            lowestPos -= startPos;

            for (int i = 0; i < blocks.Count; i++)
                blocks[i].Pos -= lowestPos;

            blocks.Sort((a, b) =>
            {
                int comp = a.Pos.Z.CompareTo(b.Pos.Z);
                if (comp == 0)
                    return a.Pos.X.CompareTo(b.Pos.X);
                else
                    return comp;
            });
        }

        protected virtual Vector3I ChoseSubPos(Vector3I pos)
            => new Vector3I(7, 3, 3);

        public virtual void Clear()
        {
            blocks.Clear();
            connections.Clear();
            values.Clear();
        }

        protected readonly record struct ConnectionRecord(ConnectTarget From, ConnectTarget To)
        {
        }

        protected readonly record struct ValueRecord(Block Block, int ValueIndex, object Value)
        {
        }
    }
}
