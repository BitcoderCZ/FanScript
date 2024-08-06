using FanScript.Compiler.Symbols;
using MathUtils.Vectors;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundConstant
    {
        public BoundConstant(object? value)
        {
            Value = value;
        }

        public object? Value { get; }

        public object GetValueOrDefault(TypeSymbol type)
        {
            if (Value is not null)
                return Value;

            if (type.IsGenericInstance)
            {
                if (type.NonGenericEquals(TypeSymbol.Array) ||
                    type.NonGenericEquals(TypeSymbol.ArraySegment))
                    return getDefault(type.InnerType);
            }

            return getDefault(type);

            object getDefault(TypeSymbol type)
            {
                if (type == TypeSymbol.Bool)
                    return false;
                else if (type == TypeSymbol.Float)
                    return 0f;
                else if (type == TypeSymbol.String)
                    return string.Empty;
                else if (type == TypeSymbol.Vector3)
                    return Vector3F.Zero;
                else if (type == TypeSymbol.Rotation)
                    return new Rotation(Vector3F.Zero);
                else
                    throw new InvalidDataException($"Unknown type: '{type}'");
            }
        }
    }
}
