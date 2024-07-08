using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundRollbackGotoStatement : BoundGotoStatement
    {
        public BoundRollbackGotoStatement(SyntaxNode syntax, BoundLabel label)
            : base(syntax, label)
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.RollbackGotoStatement;
    }
}
