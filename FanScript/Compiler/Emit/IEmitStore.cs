using System.Diagnostics;
using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit;

internal interface IEmitStore
{
    IConnectTarget In { get; }

    IEnumerable<IConnectTarget> Out { get; }
}

internal sealed class NopEmitStore : IEmitStore
{
    public static readonly NopEmitStore Instance = new NopEmitStore();

    private NopEmitStore()
    {
    }

    public IConnectTarget In => NopConnectTarget.Instance;

    public IEnumerable<IConnectTarget> Out => [];
}

internal sealed class MultiEmitStore : IEmitStore
{
    public MultiEmitStore(IEmitStore inStore, IEmitStore outStore)
    {
        InStore = inStore;
        OutStore = outStore;
    }

    public static MultiEmitStore Empty => new MultiEmitStore(NopEmitStore.Instance, NopEmitStore.Instance);

    public IEmitStore InStore { get; set; }

    public IEmitStore OutStore { get; set; }

    public IConnectTarget In => InStore.In;

    public IEnumerable<IConnectTarget> Out => OutStore.Out;
}

internal class BasicEmitStore : IEmitStore
{
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

    public BasicEmitStore(IConnectTarget @in, IEnumerable<IConnectTarget> @out)
    {
        In = @in;
        Out = @out;
    }

    public IConnectTarget In { get; init; }

    public IEnumerable<IConnectTarget> Out { get; init; }

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
        => new BasicEmitStore(new BlockConnectTarget(block, terminal), [NopConnectTarget.Instance]);

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
        => new BasicEmitStore(NopConnectTarget.Instance, [new BlockConnectTarget(block, terminal)]);
}

internal class GotoEmitStore : IEmitStore
{
    public readonly string LabelName;

    public GotoEmitStore(string name)
    {
        LabelName = name;
    }

    public IConnectTarget In => NopConnectTarget.Instance;

    public IEnumerable<IConnectTarget> Out => [];
}

internal sealed class ConditionalGotoEmitStore : BasicEmitStore
{
    public Block OnCondition;
    public Terminal OnConditionTerminal;

    public ConditionalGotoEmitStore(Block @in, Terminal inConnector, Block onCondition, Terminal onConditionConnector, Block @out, Terminal outConnector)
        : base(@in, inConnector, @out, outConnector)
    {
        OnCondition = onCondition;
        OnConditionTerminal = onConditionConnector;
    }
}

internal sealed class LabelEmitStore : IEmitStore
{
    public readonly string Name;

    public LabelEmitStore(string name)
    {
        Name = name;
    }

    public IConnectTarget In => NopConnectTarget.Instance;

    public IEnumerable<IConnectTarget> Out => [];
}

/// <summary>
/// Used by goto rollback, neccesary because special block blocks (play sensor, late update) execute after even if they execute the body, so the after would get executed twice
/// </summary>
internal class RollbackEmitStore : IEmitStore
{
    public static readonly RollbackEmitStore Instance = new RollbackEmitStore();

    protected RollbackEmitStore()
    {
    }

    public IConnectTarget In => NopConnectTarget.Instance;

    public IEnumerable<IConnectTarget> Out => [];
}

internal sealed class ReturnEmitStore : RollbackEmitStore
{
    public static new readonly ReturnEmitStore Instance = new ReturnEmitStore();
}

internal sealed class AbsoluteEmitStore : IEmitStore
{
    public AbsoluteEmitStore(Vector3I blockPos, Vector3I? voxelPos = null)
    {
        Out = [new AbsoluteConnectTarget(blockPos, voxelPos)];
    }

    public IConnectTarget In => NopConnectTarget.Instance;

    public IEnumerable<IConnectTarget> Out { get; }
}
