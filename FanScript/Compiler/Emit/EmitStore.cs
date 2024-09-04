
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

        public MultiEmitStore(EmitStore inStore, EmitStore outStore)
        {
            InStore = inStore;
            OutStore = outStore;
        }
    }

    internal class BasicEmitStore : EmitStore
    {
        public ConnectTarget In { get; init; }
        public IEnumerable<ConnectTarget> Out { get; init; }

        public BasicEmitStore(ConnectTarget @in, IEnumerable<ConnectTarget> @out)
        {
            In = @in;
            Out = @out;
        }
        public BasicEmitStore(Block block)
            : this(block, block.Type.Before, block, block.Type.After)
        {
            Debug.Assert(block.Type.Type == BlockType.Active, "block.Type.Type must be BlockType.Active");
        }
        public BasicEmitStore(Block @in, Terminal inTerminal, Block @out, Terminal outTerminal)
        {
            In = new BlockConnectTarget(@in, inTerminal);
            Out = [new BlockConnectTarget(@out, outTerminal)];
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

        public GotoEmitStore(string name)
        {
            LabelName = name;
        }
    }

    internal sealed class ConditionalGotoEmitStore : BasicEmitStore
    {
        public Block OnCondition;
        public Terminal OnConditionTerminal;

        public ConditionalGotoEmitStore(Block @in, Terminal inConnector, Block onCondition,
            Terminal onConditionConnector, Block @out, Terminal outConnector)
            : base(@in, inConnector, @out, outConnector)
        {
            OnCondition = onCondition;
            OnConditionTerminal = onConditionConnector;
        }
    }

    internal sealed class LabelEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();

        public readonly string Name;

        public LabelEmitStore(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Used by goto rollback, neccesary because special block blocks (play sensor, late update) execute after even if they execute the body, so the after would get executed twice
    /// </summary>
    internal class RollbackEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out => Enumerable.Empty<ConnectTarget>();
    }

    internal sealed class ReturnEmitStore : RollbackEmitStore
    {
    }

    internal sealed class AbsoluteEmitStore : EmitStore
    {
        public ConnectTarget In => new NopConnectTarget();
        public IEnumerable<ConnectTarget> Out { get; }

        public AbsoluteEmitStore(Vector3I blockPos, Vector3I? voxelPos = null)
        {
            Out = [new AbsoluteConnectTarget(blockPos, voxelPos)];
        }
    }
}
