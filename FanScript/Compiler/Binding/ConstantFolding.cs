﻿using FanScript.Compiler.Symbols;

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
                        return new BoundConstant((float)operand.ConstantValue.Value);
                    case BoundUnaryOperatorKind.Negation:
                        return new BoundConstant(-(float)operand.ConstantValue.Value);
                    case BoundUnaryOperatorKind.LogicalNegation:
                        return new BoundConstant(!(bool)operand.ConstantValue.Value);
                    default:
                        throw new Exception($"Unexpected unary operator {op.Kind}");
                }
            }

            return null;
        }

        public static BoundConstant? Fold(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            BoundConstant? leftConstant = left.ConstantValue;
            BoundConstant? rightConstant = right.ConstantValue;

            // Special case && and || because there are cases where only one
            // side needs to be known.

            if (op.Kind == BoundBinaryOperatorKind.LogicalAnd)
                if (leftConstant is not null && !(bool)leftConstant.Value ||
                    rightConstant is not null && !(bool)rightConstant.Value)
                    return new BoundConstant(false);

            if (op.Kind == BoundBinaryOperatorKind.LogicalOr)
                if (leftConstant is not null && (bool)leftConstant.Value ||
                    rightConstant is not null && (bool)rightConstant.Value)
                    return new BoundConstant(true);

            if (leftConstant is null || rightConstant is null)
                return null;

            object l = leftConstant.Value;
            object r = rightConstant.Value;

            TypeSymbol lt = left.Type ?? TypeSymbol.Error;
            TypeSymbol rt = right.Type ?? TypeSymbol.Error;

            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (lt == Float && rt == Float)
                        return new BoundConstant((float)l + (float)r);
                    else if (lt == Vector3 && rt == Vector3)
                        return new BoundConstant((MathUtils.Vectors.Vector3F)l + (MathUtils.Vectors.Vector3F)r);
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Subtraction:
                    if (lt == Float && rt == Float)
                        return new BoundConstant((float)l - (float)r);
                    else if (lt == Vector3 && rt == Vector3)
                        return new BoundConstant((MathUtils.Vectors.Vector3F)l - (MathUtils.Vectors.Vector3F)r);
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Multiplication:
                    if (lt == Float && rt == Float)
                        return new BoundConstant((float)l * (float)r);
                    else if (lt == Vector3 && rt == Float)
                        return new BoundConstant((MathUtils.Vectors.Vector3F)l * (float)r);
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Division:
                    if (lt == Float && rt == Float)
                        return new BoundConstant((float)l / (float)r);
                    else if (lt == Vector3 && rt == Float)
                        return new BoundConstant((MathUtils.Vectors.Vector3F)l / (float)r);
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.Modulo:
                    if (lt == Float && rt == Float)
                        return new BoundConstant((float)l % (float)r);
                    else if (lt == Vector3 && rt == Float)
                        return new BoundConstant((MathUtils.Vectors.Vector3F)l % (float)r);
                    else
                        throw new Exception($"Unexpected combination of types for {op.Kind}: {lt} and {rt}");
                case BoundBinaryOperatorKind.LogicalAnd:
                    return new BoundConstant((bool)l && (bool)r);
                case BoundBinaryOperatorKind.LogicalOr:
                    return new BoundConstant((bool)l || (bool)r);
                case BoundBinaryOperatorKind.Equals:
                    return new BoundConstant(Equals(l, r));
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundConstant(!Equals(l, r));
                case BoundBinaryOperatorKind.Less:
                    return new BoundConstant((float)l < (float)r);
                case BoundBinaryOperatorKind.LessOrEquals:
                    return new BoundConstant((float)l <= (float)r);
                case BoundBinaryOperatorKind.Greater:
                    return new BoundConstant((float)l > (float)r);
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return new BoundConstant((float)l >= (float)r);
                default:
                    throw new Exception($"Unexpected binary operator {op.Kind}");
            }
        }
    }
}
