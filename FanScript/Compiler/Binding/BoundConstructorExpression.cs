using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using MathUtils.Vectors;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundConstructorExpression : BoundExpression
    {
        public BoundConstructorExpression(SyntaxNode syntax, TypeSymbol type, BoundExpression expressionX, BoundExpression expressionY, BoundExpression expressionZ)
            : base(syntax)
        {
            Type = type;
            ExpressionX = expressionX;
            ExpressionY = expressionY;
            ExpressionZ = expressionZ;

            if (ExpressionX.ConstantValue is not null && ExpressionY.ConstantValue is not null && ExpressionZ.ConstantValue is not null)
            {
                Vector3F val = new Vector3F((float)ExpressionX.ConstantValue.GetValueOrDefault(TypeSymbol.Float), (float)ExpressionY.ConstantValue.GetValueOrDefault(TypeSymbol.Float), (float)ExpressionZ.ConstantValue.GetValueOrDefault(TypeSymbol.Float));

                ConstantValue = Type == TypeSymbol.Rotation ? new BoundConstant(new Rotation(val)) : new BoundConstant(val);
            }
        }

        public override BoundNodeKind Kind => BoundNodeKind.ConstructorExpression;

        public override TypeSymbol Type { get; }

        public override BoundConstant? ConstantValue { get; }

        public BoundExpression ExpressionX { get; }

        public BoundExpression ExpressionY { get; }

        public BoundExpression ExpressionZ { get; }
    }
}
