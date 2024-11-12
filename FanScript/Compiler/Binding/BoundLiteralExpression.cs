using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using MathUtils.Vectors;

namespace FanScript.Compiler.Binding;

internal sealed class BoundLiteralExpression : BoundExpression
{
    public BoundLiteralExpression(SyntaxNode syntax, object? value)
        : base(syntax)
    {
        Type = value is null
            ? TypeSymbol.Null
            : value is bool
            ? TypeSymbol.Bool
            : value is float
            ? TypeSymbol.Float
            : value is string
            ? TypeSymbol.String
            : value is Vector3F
            ? TypeSymbol.Vector3
            : value is Rotation ? TypeSymbol.Rotation : throw new Exception($"Unexpected literal '{value}' of type {value.GetType()}");

        ConstantValue = new BoundConstant(value);
    }

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;

    public override TypeSymbol Type { get; }

    public object? Value => ConstantValue.Value;

    public override BoundConstant ConstantValue { get; }
}
