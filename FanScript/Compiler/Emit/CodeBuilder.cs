using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit
{
    public abstract class CodeBuilder
    {
        public abstract BuildPlatformInfo PlatformInfo { get; }

        public IBlockPlacer BlockPlacer { get; protected set; }

        protected List<Block> setBlocks = new();
        protected List<MakeConnection> makeConnections = new();
        protected List<SetValue> setValues = new();

        public CodeBuilder(IBlockPlacer blockPlacer)
        {
            BlockPlacer = blockPlacer;
        }

        public virtual Block AddBlock(DefBlock defBlock)
        {
            Block block = BlockPlacer.Place(defBlock);

            setBlocks.Add(block);

            return block;
        }

        internal virtual void ConnectBlocks(EmitStore? from, EmitStore to)
        {
            if (from is NopEmitStore || to is NopEmitStore)
            {
                Console.WriteLine("Tried to connect nop store");
                return;
            }

            if (to.In is null)
                return;

            if (from?.Out is not null)
                foreach (var (block, terminal) in from.Out.Zip(from.OutTerminal))
                    ConnectBlocks(block, terminal, to.In, to.InTerminal);
        }
        public virtual void ConnectBlocks(Block[] from, Terminal[] fromTerminal, Block to, Terminal toTerminal)
        {
            if (from is not null)
                for (int i = 0; i < from.Length; i++)
                    ConnectBlocks(from[i], fromTerminal[i], to, toTerminal);
        }
        public virtual void ConnectBlocks(Block from, Terminal fromTerminal, Block to, Terminal toTerminal)
            => makeConnections.Add(new MakeConnection(from, fromTerminal, to, toTerminal));

        public virtual void SetBlockValue(Block block, int valueIndex, object value)
            => setValues.Add(new SetValue(block, valueIndex, value));

        public abstract object Build(Vector3I startPos, params object[] args);

        protected void PreBuild(Vector3I startPos)
        {
            if (startPos.X < 0 || startPos.Y < 0 || startPos.Z < 0)
                throw new ArgumentOutOfRangeException(nameof(startPos), $"{nameof(startPos)} must be >= 0");

            Vector3I lowestPos = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            for (int i = 0; i < setBlocks.Count; i++)
            {
                Vector3I pos = setBlocks[i].Pos;

                if (pos.X < lowestPos.X)
                    lowestPos.X = pos.X;
                if (pos.Y < lowestPos.Y)
                    lowestPos.Y = pos.Y;
                if (pos.Z < lowestPos.Z)
                    lowestPos.Z = pos.Z;
            }

            lowestPos -= startPos;

            for (int i = 0; i < setBlocks.Count; i++)
                setBlocks[i].Pos -= lowestPos;

            setBlocks.Sort((a, b) =>
            {
                int comp = a.Pos.Z.CompareTo(b.Pos.Z);
                if (comp == 0)
                    return a.Pos.X.CompareTo(b.Pos.X);
                else
                    return comp;
            });
        }

        public virtual void Clear()
        {
            setBlocks.Clear();
            makeConnections.Clear();
            setValues.Clear();
        }

        protected readonly record struct MakeConnection(Block Block1, Terminal Terminal1, Block Block2, Terminal Terminal2)
        {
        }

        protected readonly record struct SetValue(Block Block, int ValueIndex, object Value)
        {
        }
    }
}
