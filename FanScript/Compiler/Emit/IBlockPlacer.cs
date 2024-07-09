using FanScript.FCInfo;

namespace FanScript.Compiler.Emit
{
    public interface IBlockPlacer
    {
        int CurrentCodeBlockBlocks { get; }

        Block Place(DefBlock defBlock);

        void EnterStatementBlock();
        virtual void StatementBlock(Action action)
        {
            EnterStatementBlock();
            action();
            ExitStatementBlock();
        }
        void ExitStatementBlock();

        void EnterExpressionBlock();
        virtual void ExpressionBlock(Action action)
        {
            EnterExpressionBlock();
            action();
            ExitExpressionBlock();
        }
        void ExitExpressionBlock();
    }
}
