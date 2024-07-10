using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using MathUtils.Vectors;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public BoundLiteralExpression(SyntaxNode syntax, object value)
            : base(syntax)
        {
            if (value is bool)
                Type = TypeSymbol.Bool;
            else if (value is float)
                Type = TypeSymbol.Float;
            else if (value is Vector3F)
                Type = TypeSymbol.Vector3;
            else if (value is Rotation)
                Type = TypeSymbol.Rotation;
            else
                throw new Exception($"Unexpected literal '{value}' of type {value.GetType()}");

            ConstantValue = new BoundConstant(value);
        }

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }
    }
}
