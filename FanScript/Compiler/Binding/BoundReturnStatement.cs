using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundReturnStatement : BoundStatement
    {
        public BoundReturnStatement(SyntaxNode syntax, BoundExpression? expression)
            : base(syntax)
        {
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

        public BoundExpression? Expression { get; }
    }
}
