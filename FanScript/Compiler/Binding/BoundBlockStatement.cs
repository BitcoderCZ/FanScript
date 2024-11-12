using System.Collections.Immutable;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundBlockStatement : BoundStatement
{
    public BoundBlockStatement(SyntaxNode syntax, ImmutableArray<BoundStatement> statements)
        : base(syntax)
    {
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

    public ImmutableArray<BoundStatement> Statements { get; }

    public static BoundBlockStatement Create(BoundStatement statement)
        => statement is BoundBlockStatement block ? block : new BoundBlockStatement(statement.Syntax, [statement]);
}
