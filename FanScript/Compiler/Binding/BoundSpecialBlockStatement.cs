using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockStatement : BoundStatement
    {
        public BoundSpecialBlockStatement(SyntaxNode syntax, SpecialBlockType type, BoundArgumentClause? argumentClause, BoundBlockStatement block)
            : base(syntax)
        {
            Type = type;
            ArgumentClause = argumentClause;
            Block = block;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockStatement;
        public SpecialBlockType Type { get; }
        public BoundArgumentClause? ArgumentClause { get; }
        public BoundBlockStatement Block { get; }
    }
}
