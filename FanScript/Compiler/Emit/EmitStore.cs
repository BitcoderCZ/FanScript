﻿
using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    internal interface EmitStore
    {
        Block In { get; }
        Terminal InTerminal { get; }
        IEnumerable<Block> Out { get; }
        IEnumerable<Terminal> OutTerminal { get; }
    }

    internal sealed class NopEmitStore : EmitStore
    {
        public Block In => new Block(new Vector3I(-1, -1, -1), Blocks.Nop);
        public Terminal InTerminal => Blocks.Nop.Before;
        public IEnumerable<Block> Out => Enumerable.Empty<Block>();
        public IEnumerable<Terminal> OutTerminal => Enumerable.Empty<Terminal>();
    }

    internal sealed class MultiEmitStore : EmitStore
    {
        public static MultiEmitStore Empty => new MultiEmitStore(new NopEmitStore(), new NopEmitStore());

        public EmitStore InStore { get; set; }
        public EmitStore OutStore { get; set; }

        public Block In => InStore.In;
        public Terminal InTerminal => InStore.InTerminal;
        public IEnumerable<Block> Out => OutStore.Out;
        public IEnumerable<Terminal> OutTerminal => OutStore.OutTerminal;

        public MultiEmitStore(EmitStore _inStore, EmitStore outStore)
        {
            InStore = _inStore;
            OutStore = outStore;
        }
    }

    internal class BlockEmitStore : EmitStore
    {
        public static BlockEmitStore Default => new BlockEmitStore();

        public Block In { get; set; }
        public Terminal InTerminal { get; set; }
        public IEnumerable<Block> Out { get; set; }
        public IEnumerable<Terminal> OutTerminal { get; set; }

        protected BlockEmitStore()
        {
            In = null!;
            InTerminal = null!;
            Out = null!;
            OutTerminal = null!;
        }

        public BlockEmitStore(Block block)
            : this(block, block.Type.Before, block, block.Type.After)
        {
        }
        public BlockEmitStore(Block _in, Terminal _inTerminal, Block _out, Terminal _outTerminal)
        {
            In = _in;
            InTerminal = _inTerminal;
            Out = [_out];
            OutTerminal = [_outTerminal];
        }
        public BlockEmitStore(Block _in, Terminal _inTerminal, Block[] _out, Terminal[] _outTerminals, bool b)
        {
            if (_out is not null && _outTerminals is not null && _out.Length != _outTerminals.Length)
                throw new ArgumentException($"_outTerminals.Length ({_outTerminals.Length}) must be equal to _out.Length ({_out.Length})", "_outTerminals");

            In = _in;
            InTerminal = _inTerminal;
            Out = _out!;
            OutTerminal = _outTerminals!;
        }

        /// <summary>
        /// Creates an <see cref="EmitStore"/> with <see cref="In"/> and <see cref="InTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static BlockEmitStore CIn(Block block, Terminal terminal)
            => new BlockEmitStore(block, terminal, null!, null!);

        /// <summary>
        /// Creates an <see cref="EmitStore"/> with <see cref="Out"/> and <see cref="OutTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static BlockEmitStore COut(Block block, Terminal terminal)
            => new BlockEmitStore(null!, null!, block, terminal);
    }

    internal class GotoEmitStore : EmitStore
    {
        public Block In => new Block(new Vector3I(-1, -1, -1), Blocks.Nop);
        public Terminal InTerminal => Blocks.Nop.Before;
        public IEnumerable<Block> Out => Enumerable.Empty<Block>();
        public IEnumerable<Terminal> OutTerminal => Enumerable.Empty<Terminal>();

        public readonly string LabelName;

        public GotoEmitStore(string _name)
        {
            LabelName = _name;
        }
    }

    internal sealed class ConditionalGotoEmitStore : BlockEmitStore
    {
        public Block OnCondition;
        public Terminal OnConditionTerminal;

        public ConditionalGotoEmitStore(Block _in, Terminal _inConnector, Block _onCondition,
            Terminal _onConditionConnector, Block _out, Terminal _outConnector)
            : base(_in, _inConnector, _out, _outConnector)
        {
            OnCondition = _onCondition;
            OnConditionTerminal = _onConditionConnector;
        }
    }

    internal sealed class LabelEmitStore : EmitStore
    {
        public Block In => new Block(new Vector3I(-1, -1, -1), Blocks.Nop);
        public Terminal InTerminal => Blocks.Nop.Before;
        public IEnumerable<Block> Out => Enumerable.Empty<Block>();
        public IEnumerable<Terminal> OutTerminal => Enumerable.Empty<Terminal>();

        public readonly string Name;

        public LabelEmitStore(string _name)
        {
            Name = _name;
        }
    }
}
