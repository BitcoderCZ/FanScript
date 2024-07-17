using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockStatement : BoundStatement
    {
        public BoundSpecialBlockStatement(SyntaxNode syntax, SpecialBlockType type, ImmutableArray<BoundExpression> arguments, BoundBlockStatement block)
            : base(syntax)
        {
            Type = type;
            Arguments = arguments;
            Block = block;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockStatement;
        public SpecialBlockType Type { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public BoundBlockStatement Block { get; }
    }
}
