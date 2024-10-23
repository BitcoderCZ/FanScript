using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundRollbackGotoStatement : BoundStatement
    {
        public BoundRollbackGotoStatement(SyntaxNode syntax, BoundLabel label)
            : base(syntax)
        {
            Label = label;
        }

        public override BoundNodeKind Kind => BoundNodeKind.RollbackGotoStatement;

        public BoundLabel Label { get; }
    }
}
