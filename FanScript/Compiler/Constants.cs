using FanScript.Compiler.Symbols;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]
namespace FanScript.Compiler
{
    internal record ConstantGroup(TypeSymbol Type, string Name, Constant[] Values)
    {
    }
    internal record Constant(string Name, object Value)
    {
    }

    internal static class Constants
    {
        public static readonly ConstantGroup TOUCH_STATE = new(TypeSymbol.Float, "TOUCH_STATE", 
        [
            new Constant("TOUCHING", 0f),
            new Constant("BEGINS", 1f),
            new Constant("ENDS", 2f),
        ]);
        public static readonly ConstantGroup TOUCH_FINGER = new(TypeSymbol.Float, "TOUCH_FINGER",
        [
            new Constant("1", 0f),
            new Constant("2", 1f),
            new Constant("3", 2f),
        ]);
        public static readonly ConstantGroup BUTTON_TYPE = new(TypeSymbol.Float, "BUTTON_TYPE",
        [
            new Constant("DIRECTION", 0f),
            new Constant("BUTTON", 1f),
        ]);
        public static readonly ConstantGroup JOYSTICK_TYPE = new(TypeSymbol.Float, "JOYSTICK_TYPE",
        [
            new Constant("XZ", 0f),
            new Constant("SCREEN", 1f),
        ]);
        public static readonly ConstantGroup SOUND = new(TypeSymbol.Float, "SOUND",
        [
            new Constant("CHIRP", 0f),
            new Constant("SCRAPE", 1f),
            new Constant("SQUEEK", 2f),
            new Constant("ENGINE", 3f),
            new Constant("BUTTON", 4f),
            new Constant("BALL", 5f),
            new Constant("PIANO", 6f),
            new Constant("MARIMBA", 7f),
            new Constant("PAD", 8f),
            new Constant("BEEP", 9f),
            new Constant("PLOP", 10f),
            new Constant("FLOP", 11f),
            new Constant("SPLASH", 12f),
            new Constant("BOOM", 13f),
            new Constant("HIT", 14f),
            new Constant("CLANG", 15f),
            new Constant("JUMP", 16f),
        ]);
        // were there more in the past?, 0 is "NONE"
        public static readonly ConstantGroup RANKING = new(TypeSymbol.Float, "RANKING",
        [
            new Constant("POINTS_MOST", 2f),
            new Constant("POINST_FEWEST", 3f),
            new Constant("TIME_FASTEST", 4f),
            new Constant("TIME_LONGEST", 5f),
        ]);
        public static readonly ConstantGroup MAX_ITEMS = new(TypeSymbol.Float, "MAX_ITEMS",
        [
            new Constant("ONOFF", 1f),
            new Constant("NO_LIMIT", 101f),
        ]);
        public static readonly ConstantGroup PRICE_INCREASE = new(TypeSymbol.Float, "PRICE_INCREASE",
        [
            new Constant("FREE", 0f),
            new Constant("FIXED_10", 1f),
            new Constant("FIXED_100", 4f),
            new Constant("FIXED_1000", 7f),
            new Constant("FIXED_10_000", 10f),
            new Constant("LINEAR_10", 2f),
            new Constant("LINEAR_100", 5f),
            new Constant("LINEAR_1000", 8f),
            new Constant("DOUBLE_10", 3f),
            new Constant("DOUBLE_100", 6f),
            new Constant("DOUBLE_1000", 9f),
        ]);

        private static IEnumerable<ConstantGroup>? groupCache;
        public static IEnumerable<ConstantGroup> Groups
            => groupCache ??= typeof(Constants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null) as ConstantGroup)
            .Where(val => val is ConstantGroup)
            .Select(group => group!);
    }
}
