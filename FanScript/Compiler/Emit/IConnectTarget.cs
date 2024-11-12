using System.Runtime.CompilerServices;
using FanScript.FCInfo;
using MathUtils.Vectors;

[assembly: InternalsVisibleTo("FanScript.Tests")]

namespace FanScript.Compiler.Emit;

public interface IConnectTarget
{
    Vector3I Pos { get; }

    int TerminalIndex { get; }

    Vector3I? VoxelPos { get; }
}

internal sealed class NopConnectTarget : IConnectTarget
{
    public static readonly NopConnectTarget Instance = new NopConnectTarget();

    private NopConnectTarget()
    {
    }

    public Vector3I Pos => new Vector3I(-1, -1, -1);

    public int TerminalIndex => -1;

    public Vector3I? VoxelPos => new Vector3I(-1, -1, -1);
}

internal sealed class BlockConnectTarget : IConnectTarget
{
    public readonly Block Block;
    public readonly Terminal Terminal;

    public BlockConnectTarget(Block block, Terminal terminal)
    {
        Block = block;
        Terminal = terminal;
    }

    public Vector3I Pos => Block.Pos;

    public int TerminalIndex => Terminal.Index;

    public Vector3I? VoxelPos => Terminal.Pos;
}

internal sealed class BlockVoxelConnectTarget : IConnectTarget
{
    public readonly Block Block;

    public BlockVoxelConnectTarget(Block block, Vector3I? voxelPos = null)
    {
        Block = block;
        VoxelPos = voxelPos ?? new Vector3I(7, 3, 3);
    }

    public Vector3I Pos => Block.Pos;

    public int TerminalIndex { get; init; }

    public Vector3I? VoxelPos { get; init; }
}

internal sealed class AbsoluteConnectTarget : IConnectTarget
{
    public AbsoluteConnectTarget(Vector3I pos, Vector3I? voxelPos = null)
    {
        Pos = pos;
        VoxelPos = voxelPos;
    }

    public Vector3I Pos { get; init; }

    public int TerminalIndex { get; init; }

    public Vector3I? VoxelPos { get; init; }
}

#pragma warning disable SA1204 // Static elements should appear before instance elements
internal static class ConnectTargetE
#pragma warning restore SA1204
{
    public static WireType GetWireType(this IConnectTarget connectTarget)
        => connectTarget switch
        {
            BlockConnectTarget blockTarget => blockTarget.Terminal.WireType,
            _ => WireType.Error,
        };
}
