using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(SyntaxNode syntax, BoundUnaryOperator op, BoundExpression operand)
            : base(syntax)
        {
            Op = op;
            Operand = operand;
            ConstantValue = ConstantFolding.Fold(op, operand);
        }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;

        public override TypeSymbol Type => Op.Type;

        public BoundUnaryOperator Op { get; }

        public BoundExpression Operand { get; }

        public override BoundConstant? ConstantValue { get; }
    }
}
