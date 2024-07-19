using FanScript.Compiler.Symbols;
using System.Reflection;

namespace FanScript.Compiler
{
    internal class Constant
    {
        public readonly TypeSymbol Type;
        public readonly string Name;
        public readonly object Value;

        public Constant(TypeSymbol type, string name, object value)
        {
            Type = type;
            Name = name;
            Value = value;
        }
    }

    internal static class Constants
    {
        public static readonly Constant TOUCH_STATE_TOUCHING = new Constant(TypeSymbol.Float, "TOUCH_STATE_TOUCHING", 0f);
        public static readonly Constant TOUCH_STATE_BEGINS = new Constant(TypeSymbol.Float, "TOUCH_STATE_BEGINS", 1f);
        public static readonly Constant TOUCH_STATE_ENDS = new Constant(TypeSymbol.Float, "TOUCH_STATE_ENDS", 2f);
        public static readonly Constant TOUCH_INDEX_1 = new Constant(TypeSymbol.Float, "TOUCH_INDEX_1", 0f);
        public static readonly Constant TOUCH_INDEX_2 = new Constant(TypeSymbol.Float, "TOUCH_INDEX_2", 1f);
        public static readonly Constant TOUCH_INDEX_3 = new Constant(TypeSymbol.Float, "TOUCH_INDEX_3", 2f);

        public static readonly Constant BUTTON_TYPE_DIRECTION = new Constant(TypeSymbol.Float, "BUTTON_TYPE_DIRECTION", 0f);
        public static readonly Constant BUTTON_TYPE_BUTTON = new Constant(TypeSymbol.Float, "BUTTON_TYPE_BUTTON", 1f);

        public static readonly Constant JOYSTICK_TYPE_XZ = new Constant(TypeSymbol.Float, "JOYSTICK_TYPE_XZ", 0f);
        public static readonly Constant JOYSTICK_TYPE_SCREEN = new Constant(TypeSymbol.Float, "JOYSTICK_TYPE_SCREEN", 1f);

        private static IEnumerable<Constant>? cache;
        public static IEnumerable<Constant> GetAll()
            => cache ??= typeof(Constants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null) as Constant)
            .Where(val => val is Constant)
            .Select(constant => constant!);
    }
}
