using System.Collections.Immutable;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public BoundBlockStatement(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
            : base(syntax)
        {
            Statements = statements;
        }

        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

        public ImmutableArray<BoundStatement> Statements { get; }

#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        public static BoundBlockStatement Create(BoundStatement statement)
            => statement is BoundBlockStatement block ? block : new BoundBlockStatement(statement.Syntax, [statement]);
#pragma warning restore SA1010
    }
}
