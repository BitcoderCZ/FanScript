using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundStatementExpression : BoundExpression
    {
        public BoundStatementExpression(SyntaxNode syntax, BoundStatement statement) : base(syntax)
        {
            Statement = statement;
        }

        public override TypeSymbol Type => TypeSymbol.Void;
        public override BoundNodeKind Kind => BoundNodeKind.StatementExpression;

        public BoundStatement Statement { get; }
    }
}
