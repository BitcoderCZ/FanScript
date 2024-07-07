using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.FCInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
                /*case SyntaxKind.Vector3Keyword:
                    return WireType.Vec3;*/
                case SyntaxKind.KeywordBool:
                    return WireType.Bool;
                default:
                    throw new Exception($"Cannot convert SyntaxKind \"{Enum.GetName(typeof(SyntaxKind), syntax)}\" to WireType");
            }
        }
        public static TypeSymbol ToTypeSymbol(this SyntaxKind syntax)
        {
            switch (syntax)
            {
                case SyntaxKind.KeywordFloat:
                    return TypeSymbol.Float;
                /*case SyntaxKind.Vector3Keyword:
                    return TypeSymbol.Vector3;*/
                case SyntaxKind.KeywordBool:
                    return TypeSymbol.Bool;
                default:
                    throw new Exception($"Cannot convert SyntaxKind \"{Enum.GetName(typeof(SyntaxKind), syntax)}\" to TypeSymbol");
            }
        }

        public static WireType ToWireType(this TypeSymbol syntax)
        {
            if (syntax == TypeSymbol.Float)
                return WireType.Float;
            /*else if (syntax == TypeSymbol.Vector3)
                return WireType.Vec3;*/
            else if (syntax == TypeSymbol.Bool)
                return WireType.Bool;
            else
                throw new Exception($"Cannot convert TypeSymbol \"{Enum.GetName(typeof(TypeSymbol), syntax)}\" to WireType");
        }

        public static string ToBlockValue(this object o)
        {
            if (o == null)
                return "Null";
            else if (o is string s)
                return $"\"{s}\"";
            else if (o is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            else if (o is bool b)
                return b.ToString().ToLower();
            else if (o is Vector3 v)
                return $"[{v.X.ToString(CultureInfo.InvariantCulture)},{v.Y.ToString(CultureInfo.InvariantCulture)},{v.Z.ToString(CultureInfo.InvariantCulture)}]";
            else
                throw new Exception($"Cannot convert object of type: \"{o.GetType()}\" to Block Value");
        }
    }
}
