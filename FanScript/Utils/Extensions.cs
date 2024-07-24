using FanScript.Compiler;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.FCInfo;
using System.Globalization;
using System.Numerics;

namespace FanScript.Utils
{
    public static class Extensions
    {
        public static WireType ToWireType(this SyntaxKind syntax)
        {
            switch (syntax)
            {
                case SyntaxKind.KeywordFloat:
                    return WireType.Float;
                case SyntaxKind.KeywordVector3:
                    return WireType.Vec3;
                case SyntaxKind.KeywordRotation:
                    return WireType.Rot;
                case SyntaxKind.KeywordBool:
                    return WireType.Bool;
                default:
                    throw new Exception($"Cannot convert SyntaxKind '{syntax}' to WireType");
            }
        }
        public static TypeSymbol ToTypeSymbol(this SyntaxKind syntax)
        {
            switch (syntax)
            {
                case SyntaxKind.KeywordFloat:
                    return TypeSymbol.Float;
                case SyntaxKind.KeywordVector3:
                    return TypeSymbol.Vector3;
                case SyntaxKind.KeywordRotation:
                    return TypeSymbol.Rotation;
                case SyntaxKind.KeywordBool:
                    return TypeSymbol.Bool;
                default:
                    throw new Exception($"Cannot convert SyntaxKind '{syntax}' to TypeSymbol");
            }
        }

        public static WireType ToWireType(this TypeSymbol type)
        {
            if (type.IsGenericInstance)
            {
                if (type.NonGenericEquals(TypeSymbol.Array))
                    return type.InnerType.ToWireType();
            }
            else if (type == TypeSymbol.Float)
                return WireType.Float;
            else if (type == TypeSymbol.Vector3)
                return WireType.Vec3;
            else if (type == TypeSymbol.Rotation)
                return WireType.Rot;
            else if (type == TypeSymbol.Bool)
                return WireType.Bool;
            else if (type == TypeSymbol.Object)
                return WireType.Obj;

            throw new Exception($"Cannot convert TypeSymbol '{type}' to WireType");
        }

        public static string ToBlockValue(this object o)
        {
            if (o is null)
                return "Null";
            else if (o is string s)
                return $"\"{s}\"";
            else if (o is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            else if (o is bool b)
                return b.ToString().ToLowerInvariant();
            else if (o is Vector3 v)
                return $"[{v.X.ToString(CultureInfo.InvariantCulture)},{v.Y.ToString(CultureInfo.InvariantCulture)},{v.Z.ToString(CultureInfo.InvariantCulture)}]";
            else if (o is Rotation r)
                return $"[{r.Value.X.ToString(CultureInfo.InvariantCulture)},{r.Value.Y.ToString(CultureInfo.InvariantCulture)},{r.Value.Z.ToString(CultureInfo.InvariantCulture)}]";
            else
                throw new Exception($"Cannot convert object of type: '{o.GetType()}' to Block Value");
        }

        public static TValue AddIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (!dict.TryGetValue(key, out TValue? val))
            {
                val = defaultValue;
                dict.Add(key, val);
            }

            return val;
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, (TResult, bool, bool)> selector)
        {
            var firstEnumerator = first.GetEnumerator();
            var secondEnumerator = second.GetEnumerator();

            bool moveFirst = true;
            bool moveSecond = true;

            while ((!moveFirst || firstEnumerator.MoveNext()) && (!moveSecond || secondEnumerator.MoveNext()))
            {
                (TResult result, moveFirst, moveSecond) = selector(firstEnumerator.Current, secondEnumerator.Current);

                yield return result;
            }
        }
    }
}
