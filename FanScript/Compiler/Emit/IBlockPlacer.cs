using FanScript.FCInfo;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    public interface IBlockPlacer
    {
        Vector3I StartPos { get; }
        int CurrentCodeBlockBlocks { get; }

        Vector3I Place(DefBlock defBlock);

        void EnterStatementBlock();
        void ExitStatementBlock();

        void EnterExpression();
        void ExitExpression();
    }
}
