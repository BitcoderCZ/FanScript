using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCompoundAssignmentStatement : BoundStatement
    {
        public BoundCompoundAssignmentStatement(SyntaxNode syntax, VariableSymbol variable, BoundBinaryOperator op, BoundExpression expression)
            : base(syntax)
        {
            Variable = variable;
            Op = op;
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentStatement;

        public VariableSymbol Variable { get; }

        public BoundBinaryOperator Op { get; }

        public BoundExpression Expression { get; }
    }
}
