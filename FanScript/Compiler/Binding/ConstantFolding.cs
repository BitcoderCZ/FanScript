using FanScript.Compiler.Symbols;
using MathUtils.Vectors;
using static FanScript.Compiler.Symbols.TypeSymbol;

namespace FanScript.Compiler.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? Fold(BoundUnaryOperator op, BoundExpression operand)
        {
            if (operand.ConstantValue is not null)
            {
                switch (op.Kind)
                {
                    case BoundUnaryOperatorKind.Identity:
                        if (op.Type == Float)
                            return new BoundConstant((float)operand.ConstantValue.Value!);
                        else if (op.Type == Vector3)
                            return new BoundConstant((Vector3F)operand.ConstantValue.Value!);
                        else if (op.Type == TypeSymbol.Rotation)
                            return new BoundConstant((Rotation)operand.ConstantValue.Value!);
                        else
                            throw new Exception($"Unexpected of type for {op.Kind}: {op.Type}");
                    case BoundUnaryOperatorKind.Negation:
                        if (op.Type == Float)
                            return new BoundConstant(-(float)operand.ConstantValue.Value!);
                        else if (op.Type == Vector3)
                            return new BoundConstant(-(Vector3F)operand.ConstantValue.Value!);
                        else if (op.Type == TypeSymbol.Rotation)
                            return null; // couldn't figure out how the inverse works, imo should be Quaterion Inverse, but that gave me differend results (yes I did convert deg-rad and back))
                        else
                            throw new Exception($"Unexpected of type for {op.Kind}: {op.Type}");
                    case BoundUnaryOperatorKind.LogicalNegation:
                        return new BoundConstant(!(bool)operand.ConstantValue.Value!);
                    default:
                        throw new Exception($"Unexpected unary operator {op.Kind}");
                }
            }

            return null;
        }

        public static BoundConstant? Fold(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            BoundConstant? l = left.ConstantValue;
            BoundConstant? r = right.ConstantValue;

            // Special case && and || because there are cases where only one
            // side needs to be known.

            if (op.Kind == BoundBinaryOperatorKind.LogicalAnd)
                if (l is not null && !(bool)l.GetValueOrDefault(Bool) ||
                    r is not null && !(bool)r.GetValueOrDefault(Bool))
                    return new BoundConstant(false);

            if (op.Kind == BoundBinaryOperatorKind.LogicalOr)
                if (l is not null && (bool)l.GetValueOrDefault(Bool) ||
                    r is not null && (bool)r.GetValueOrDefault(Bool))
                    return new BoundConstant(true);

            if (l is null || r is null)
                return null;

            TypeSymbol lt = left.Type;
            TypeSymbol rt = right.Type;
            TypeSymbol t = op.Type;
            TypeSymbol notNullType = lt == Null ? rt : lt;

            if (lt == Null && rt == Null)
            {
                switch (op.Kind)
                {
                    case BoundBinaryOperatorKind.Equals:
                        return new BoundConstant(true);
                    case BoundBinaryOperatorKind.NotEquals:
                        return new BoundConstant(false);
                }
            }

            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (t == Float)
                        return new BoundConstant((float)l.GetValueOrDefault(Float) + (float)r.GetValueOrDefault(Float));
                    else if (t == Vector3)
                        return new BoundConstant((Vector3F)l.GetValueOrDefault(Vector3) + (Vector3F)r.GetValueOrDefault(Vector3));
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Subtraction:
                    if (t == Float)
                        return new BoundConstant((float)l.GetValueOrDefault(Float) - (float)r.GetValueOrDefault(Float));
                    else if (t == Vector3)
                        return new BoundConstant((Vector3F)l.GetValueOrDefault(Vector3) - (Vector3F)r.GetValueOrDefault(Vector3));
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Multiplication:
                    if (t == Float)
                        return new BoundConstant((float)l.GetValueOrDefault(Float) * (float)r.GetValueOrDefault(Float));
                    else if (t == Vector3)
                        return new BoundConstant((Vector3F)l.GetValueOrDefault(Vector3) * (float)r.GetValueOrDefault(Float));
                    else if (lt == Vector3 && rt == TypeSymbol.Rotation)
                        return null; // TODO
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Division:
                    if (t == Float)
                        return new BoundConstant((float)l.GetValueOrDefault(Float) / (float)r.GetValueOrDefault(Float));
                    else if (t == Vector3)
                        return new BoundConstant((Vector3F)l.GetValueOrDefault(Vector3) / (float)r.GetValueOrDefault(Float));
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Modulo:
                    if (t == Float)
                        return new BoundConstant((float)l.GetValueOrDefault(Float) % (float)r.GetValueOrDefault(Float));
                    else if (t == Vector3)
                        return new BoundConstant((Vector3F)l.GetValueOrDefault(Vector3) % (float)r.GetValueOrDefault(Float));
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.LogicalAnd:
                    return new BoundConstant((bool)l.GetValueOrDefault(Bool) && (bool)r.GetValueOrDefault(Bool));
                case BoundBinaryOperatorKind.LogicalOr:
                    return new BoundConstant((bool)l.GetValueOrDefault(Bool) || (bool)r.GetValueOrDefault(Bool));
                case BoundBinaryOperatorKind.Equals:
                    return new BoundConstant(Equals(l.GetValueOrDefault(notNullType), r.GetValueOrDefault(notNullType)));
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundConstant(!Equals(l.GetValueOrDefault(notNullType), r.GetValueOrDefault(notNullType)));
                case BoundBinaryOperatorKind.Less:
                    return new BoundConstant((float)l.GetValueOrDefault(Float) < (float)r.GetValueOrDefault(Float));
                case BoundBinaryOperatorKind.LessOrEquals:
                    return new BoundConstant((float)l.GetValueOrDefault(Float) <= (float)r.GetValueOrDefault(Float));
                case BoundBinaryOperatorKind.Greater:
                    return new BoundConstant((float)l.GetValueOrDefault(Float) > (float)r.GetValueOrDefault(Float));
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return new BoundConstant((float)l.GetValueOrDefault(Float) >= (float)r.GetValueOrDefault(Float));
                default:
                    throw new Exception($"Unexpected binary operator {op.Kind}");
            }
        }
    }
}
