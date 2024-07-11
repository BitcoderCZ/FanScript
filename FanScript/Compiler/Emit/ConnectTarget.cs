using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    public interface ConnectTarget
    {
        Vector3I Pos { get; }

        int TerminalIndex { get; }
        Vector3I? SubPos { get; }
    }

    internal sealed class NopConnectTarget : ConnectTarget
    {
        public Vector3I Pos => new Vector3I(-1, -1, -1);

        public int TerminalIndex => -1;
        public Vector3I? SubPos => new Vector3I(-1, -1, -1);
    }

    internal sealed class BlockConnectTarget : ConnectTarget
    {
        public Vector3I Pos => Block.Pos;

        public int TerminalIndex => Terminal.Index;
        public Vector3I? SubPos => Terminal.Pos;

        public readonly Block Block;
        public readonly Terminal Terminal;

        public BlockConnectTarget(Block _block, Terminal _terminal)
        {
            Block = _block;
            Terminal = _terminal;
        }
    }

    internal sealed class AbsoluteConnectTarget : ConnectTarget
    {
        public Vector3I Pos { get; init; }

        public int TerminalIndex { get; init; }

        public Vector3I? SubPos { get; init; }

        public AbsoluteConnectTarget(Vector3I _pos, Vector3I? _subPos = null)
        {
            Pos = _pos;
            SubPos = _subPos;
        }
    }
}
