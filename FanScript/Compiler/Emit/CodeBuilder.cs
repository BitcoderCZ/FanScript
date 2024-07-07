using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    public abstract class CodeBuilder
    {
        public abstract BuildPlatformInfo PlatformInfo { get; }

        public IBlockPlacer BlockPlacer { get; protected set; }

        protected List<SetBlock> setBlocks = new();
        protected List<MakeConnection> makeConnections = new();
        protected List<SetValue> setValues = new();

        public CodeBuilder(IBlockPlacer blockPlacer)
        {
            BlockPlacer = blockPlacer;
        }

        public virtual Block AddBlock(DefBlock defBlock)
        {
            Block block = new Block(BlockPlacer.Place(defBlock), defBlock);

            setBlocks.Add(new SetBlock(defBlock.Id, block));

            return block;
        }

        internal virtual void ConnectBlocks(EmitStore? from, EmitStore to)
        {
            if (from?.Out is not null)
                for (int i = 0; i < from.Out.Length; i++)
                    ConnectBlocks(from.Out[i], from.OutTerminal[i], to.In, to.InTerminal);
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

        public abstract object Build(params object[] args);

        protected void PreBuild()
        {
            Vector3I lowestPos = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            for (int i = 0; i < setBlocks.Count; i++)
            {
                Vector3I pos = setBlocks[i].Block.Pos;

                if (pos.X < lowestPos.X)
                    lowestPos.X = pos.X;
                if (pos.Y < lowestPos.Y)
                    lowestPos.Y = pos.Y;
                if (pos.Z < lowestPos.Z)
                    lowestPos.Z = pos.Z;
            }

            lowestPos -= BlockPlacer.StartPos; 

            for (int i = 0; i < setBlocks.Count; i++)
                setBlocks[i].Block.Pos -= lowestPos;

            setBlocks.Sort((a, b) =>
            {
                int comp = a.Block.Pos.Z.CompareTo(b.Block.Pos.Z);
                if (comp == 0)
                    return a.Block.Pos.X.CompareTo(b.Block.Pos.X);
                else
                    return comp;
            });

            if (setBlocks.Count > 0 && setBlocks[0].Block.Pos != Vector3I.Zero)
                setBlocks.Insert(0, new SetBlock(1, new Block(Vector3I.Zero, null!)));
        }

        protected void Clear()
        {
            setBlocks.Clear();
            makeConnections.Clear();
            setValues.Clear();
        }

        protected record SetBlock(ushort Id, Block Block)
        {
        }

        protected record MakeConnection(Block Block1, Terminal Terminal1, Block Block2, Terminal Terminal2)
        {
        }

        protected record SetValue(Block Block, int ValueIndex, object Value)
        {
        }
    }
}
