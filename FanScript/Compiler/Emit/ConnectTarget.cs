using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.Tests")]
namespace FanScript.Compiler.Emit
{
    public interface ConnectTarget
    {
        Vector3I Pos { get; }

        int TerminalIndex { get; }
        Vector3I? VoxelPos { get; }
    }

    internal sealed class NopConnectTarget : ConnectTarget
    {
        public Vector3I Pos => new Vector3I(-1, -1, -1);

        public int TerminalIndex => -1;
        public Vector3I? VoxelPos => new Vector3I(-1, -1, -1);
    }

    internal sealed class BlockConnectTarget : ConnectTarget
    {
        public Vector3I Pos => Block.Pos;

        public int TerminalIndex => Terminal.Index;
        public Vector3I? VoxelPos => Terminal.Pos;

        public readonly Block Block;
        public readonly Terminal Terminal;

        public BlockConnectTarget(Block block, Terminal terminal)
        {
            Block = block;
            Terminal = terminal;
        }
    }

    internal sealed class AbsoluteConnectTarget : ConnectTarget
    {
        public Vector3I Pos { get; init; }

        public int TerminalIndex { get; init; }

        public Vector3I? VoxelPos { get; init; }

        public AbsoluteConnectTarget(Vector3I pos, Vector3I? voxelPos = null)
        {
            Pos = pos;
            VoxelPos = voxelPos;
        }
    }
}
