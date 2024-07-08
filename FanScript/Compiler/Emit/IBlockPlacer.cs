using FanScript.FCInfo;

namespace FanScript.Compiler.Emit
{
    public interface IBlockPlacer
    {
        int CurrentCodeBlockBlocks { get; }

        Block Place(DefBlock defBlock);

        void EnterStatementBlock();
        void ExitStatementBlock();

        void EnterExpression();
        void ExitExpression();
    }
}
