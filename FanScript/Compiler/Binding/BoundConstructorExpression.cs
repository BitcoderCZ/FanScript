using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Vector3F val = new Vector3F((float)ExpressionX.ConstantValue.Value, (float)ExpressionY.ConstantValue.Value, (float)ExpressionZ.ConstantValue.Value);

                if (Type == TypeSymbol.Rotation)
                    ConstantValue = new BoundConstant(new Rotation(val));
                else
                    ConstantValue = new BoundConstant(val);
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
