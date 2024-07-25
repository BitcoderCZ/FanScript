using FanScript.FCInfo;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler.Emit
{
    public interface IBlockPlacer
    {
        int CurrentCodeBlockBlocks { get; }

        Block Place(BlockDef blockDef);

        void EnterStatementBlock();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        virtual void StatementBlock(Action action)
        {
            EnterStatementBlock();
            action();
            ExitStatementBlock();
        }
        void ExitStatementBlock();

        void EnterExpressionBlock();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        virtual void ExpressionBlock(Action action)
        {
            EnterExpressionBlock();
            action();
            ExitExpressionBlock();
        }
        void ExitExpressionBlock();
    }
}
