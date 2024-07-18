
using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FanScript.Compiler.Emit
{
    internal interface EmitStore
    {
        ConnectTarget In { get; }
        IEnumerable<ConnectTarget> Out { get; }
    }

    internal sealed class NopEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();
    }

    internal sealed class MultiEmitStore : EmitStore
    {
        public static MultiEmitStore Empty => new MultiEmitStore(new NopEmitStore(), new NopEmitStore());

        public EmitStore InStore { get; set; }
        public EmitStore OutStore { get; set; }

        public ConnectTarget In => InStore.In;
        public IEnumerable<ConnectTarget> Out => OutStore.Out;

        public MultiEmitStore(EmitStore _inStore, EmitStore outStore)
        {
            InStore = _inStore;
            OutStore = outStore;
        }
    }

    internal class BasicEmitStore : EmitStore
    {
        public ConnectTarget In { get; init; }
        public IEnumerable<ConnectTarget> Out { get; init; }

        public BasicEmitStore(Block block)
            : this(block, block.Type.Before, block, block.Type.After)
        {
            Debug.Assert(block.Type.Type == BlockType.Active, "block.Type.Type must be BlockType.Active");
        }
        public BasicEmitStore(Block _in, Terminal _inTerminal, Block _out, Terminal _outTerminal)
        {
            In = new BlockConnectTarget(_in, _inTerminal);
            Out = [new BlockConnectTarget(_out, _outTerminal)];
        }

        /// <summary>
        /// Creates an <see cref="BasicEmitStore"/> with <see cref="In"/> and <see cref="InTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BasicEmitStore CIn(Block block)
        {
            Debug.Assert(block.Type.Type == BlockType.Active, "block.Type.Type must be BlockType.Active");
            return CIn(block, block.Type.Before);
        }
        /// <summary>
        /// Creates an <see cref="BasicEmitStore"/> with <see cref="In"/> and <see cref="InTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static BasicEmitStore CIn(Block block, Terminal terminal)
            => new BasicEmitStore(block, terminal, null!, null!);

        /// <summary>
        /// Creates an <see cref="BasicEmitStore"/> with <see cref="Out"/> and <see cref="OutTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static BasicEmitStore COut(Block block)
        {
            Debug.Assert(block.Type.Type == BlockType.Active, "block.Type.Type must be BlockType.Active");
            return COut(block, block.Type.After);
        }
        /// <summary>
        /// Creates an <see cref="BasicEmitStore"/> with <see cref="Out"/> and <see cref="OutTerminal"/> assigned
        /// </summary>
        /// <param name="block"></param>
        /// <param name="terminal"></param>
        /// <returns></returns>
        public static BasicEmitStore COut(Block block, Terminal terminal)
            => new BasicEmitStore(null!, null!, block, terminal);
    }

    internal class GotoEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();

        public readonly string LabelName;

        public GotoEmitStore(string _name)
        {
            LabelName = _name;
        }
    }

    internal sealed class ConditionalGotoEmitStore : BasicEmitStore
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
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();

        public readonly string Name;

        public LabelEmitStore(string _name)
        {
            Name = _name;
        }
    }

    /// <summary>
    /// Used by goto rollback, neccesary because special block blocks (play sensor, late update) execute after even if they execute the body, so the after would get executed twice
    /// </summary>
    internal sealed class RollbackEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();
    }

    internal sealed class AbsoluteEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out { get; }

        public AbsoluteEmitStore(Vector3I _blockPos, Vector3I? _subPos = null)
        {
            Out = [new AbsoluteConnectTarget(_blockPos, _subPos)];
        }
    }
}
