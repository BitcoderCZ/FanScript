using FanScript.Compiler.Symbols;
using FanScript.FCInfo;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]
namespace FanScript.Utils
{
    internal static class Extensions
    {
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
            else if (type == TypeSymbol.Constraint)
                return WireType.Con;

            throw new Exception($"Cannot convert TypeSymbol '{type}' to WireType");
        }

        public static bool IsCurrentLineEmpty(this StringBuilder builder)
        {
            int len = builder.Length;
            if (len == 0)
                return true;

            char last = builder[len - 1];

            return last == '\n' || last == '\r';
        }
    }
}
