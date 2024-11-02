using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Emit
{
    public abstract class CodePlacer
    {
        protected readonly BlockBuilder Builder;

        protected CodePlacer(BlockBuilder builder)
        {
            Builder = builder;
        }

        public abstract int CurrentCodeBlockBlocks { get; }

        public abstract Block PlaceBlock(BlockDef blockDef);

        public abstract void EnterStatementBlock();

        public IDisposable StatementBlock()
        {
            EnterStatementBlock();
            return new Disposable(ExitStatementBlock);
        }

        public abstract void ExitStatementBlock();

        public abstract void EnterExpressionBlock();

        public IDisposable ExpressionBlock()
        {
            EnterExpressionBlock();
            return new Disposable(ExitExpressionBlock);
        }

        public abstract void ExitExpressionBlock();

        public abstract void EnterHighlight();

        public abstract void ExitHightlight();
    }
}
