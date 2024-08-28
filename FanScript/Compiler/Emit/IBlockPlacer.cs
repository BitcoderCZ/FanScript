using FanScript.FCInfo;
using FanScript.Utils;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler.Emit
{
    public interface IBlockPlacer
    {
        int CurrentCodeBlockBlocks { get; }

        Block Place(BlockDef blockDef);

        void EnterStatementBlock();
        virtual IDisposable StatementBlock()
        {
            EnterStatementBlock();
            return new Disposable(ExitStatementBlock);
        }
        void ExitStatementBlock();

        void EnterExpressionBlock();
        virtual IDisposable ExpressionBlock()
        {
            EnterExpressionBlock();
            return new Disposable(ExitExpressionBlock);
        }
        void ExitExpressionBlock();
    }
}
