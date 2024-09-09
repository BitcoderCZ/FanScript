using FanScript.Compiler.Symbols;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]
namespace FanScript.Compiler
{
    internal record Constant(TypeSymbol Type, string Name, object Value)
    {
    }

    internal static class Constants
    {
        public static readonly Constant TOUCH_STATE_TOUCHING = new Constant(TypeSymbol.Float, "TOUCH_STATE_TOUCHING", 0f);
        public static readonly Constant TOUCH_STATE_BEGINS = new Constant(TypeSymbol.Float, "TOUCH_STATE_BEGINS", 1f);
        public static readonly Constant TOUCH_STATE_ENDS = new Constant(TypeSymbol.Float, "TOUCH_STATE_ENDS", 2f);
        public static readonly Constant TOUCH_FINGER_1 = new Constant(TypeSymbol.Float, "TOUCH_FINGER_1", 0f);
        public static readonly Constant TOUCH_FINGER_2 = new Constant(TypeSymbol.Float, "TOUCH_FINGER_2", 1f);
        public static readonly Constant TOUCH_FINGER_3 = new Constant(TypeSymbol.Float, "TOUCH_FINGER_3", 2f);

        public static readonly Constant BUTTON_TYPE_DIRECTION = new Constant(TypeSymbol.Float, "BUTTON_TYPE_DIRECTION", 0f);
        public static readonly Constant BUTTON_TYPE_BUTTON = new Constant(TypeSymbol.Float, "BUTTON_TYPE_BUTTON", 1f);

        public static readonly Constant JOYSTICK_TYPE_XZ = new Constant(TypeSymbol.Float, "JOYSTICK_TYPE_XZ", 0f);
        public static readonly Constant JOYSTICK_TYPE_SCREEN = new Constant(TypeSymbol.Float, "JOYSTICK_TYPE_SCREEN", 1f);

        public static readonly Constant SOUND_CHIRP = new Constant(TypeSymbol.Float, "SOUND_CHIRP", 0f);
        public static readonly Constant SOUND_SCRAPE = new Constant(TypeSymbol.Float, "SOUND_SCRAPE", 1f);
        public static readonly Constant SOUND_SQUEEK = new Constant(TypeSymbol.Float, "SOUND_SQUEEK", 2f);
        public static readonly Constant SOUND_ENGINE = new Constant(TypeSymbol.Float, "SOUND_ENGINE", 3f);
        public static readonly Constant SOUND_BUTTON = new Constant(TypeSymbol.Float, "SOUND_BUTTON", 4f);
        public static readonly Constant SOUND_BALL = new Constant(TypeSymbol.Float, "SOUND_BALL", 5f);
        public static readonly Constant SOUND_PIANO = new Constant(TypeSymbol.Float, "SOUND_PIANO", 6f);
        public static readonly Constant SOUND_MARIMBA = new Constant(TypeSymbol.Float, "SOUND_MARIMBA", 7f);
        public static readonly Constant SOUND_PAD = new Constant(TypeSymbol.Float, "SOUND_PAD", 8f);
        public static readonly Constant SOUND_BEEP = new Constant(TypeSymbol.Float, "SOUND_BEEP", 9f);
        public static readonly Constant SOUND_PLOP = new Constant(TypeSymbol.Float, "SOUND_PLOP", 10f);
        public static readonly Constant SOUND_FLOP = new Constant(TypeSymbol.Float, "SOUND_FLOP", 11f);
        public static readonly Constant SOUND_SPLASH = new Constant(TypeSymbol.Float, "SOUND_SPLASH", 12f);
        public static readonly Constant SOUND_BOOM = new Constant(TypeSymbol.Float, "SOUND_BOOM", 13f);
        public static readonly Constant SOUND_HIT = new Constant(TypeSymbol.Float, "SOUND_HIT", 14f);
        public static readonly Constant SOUND_CLANG = new Constant(TypeSymbol.Float, "SOUND_CLANG", 15f);
        public static readonly Constant SOUND_JUMP = new Constant(TypeSymbol.Float, "SOUND_JUMP", 16f);

        // were there more in the past?, 0 is "NONE"
        public static readonly Constant RANKING_POINTS_MOST = new Constant(TypeSymbol.Float, "RANKING_POINTS_MOST", 2f);
        public static readonly Constant RANKING_POINST_FEWEST = new Constant(TypeSymbol.Float, "RANKING_POINST_FEWEST", 3f);
        public static readonly Constant RANKING_TIME_FASTEST = new Constant(TypeSymbol.Float, "RANKING_TIME_FASTEST", 4f);
        public static readonly Constant RANKING_TIME_LONGEST = new Constant(TypeSymbol.Float, "RANKING_TIME_LONGEST", 5f);

        public static readonly Constant MAX_ITEMS_ONOFF = new Constant(TypeSymbol.Float, "MAX_ITEMS_ONOFF", 1f);
        public static readonly Constant MAX_ITEMS_NO_LIMIT = new Constant(TypeSymbol.Float, "MAX_ITEMS_NO_LIMIT", 101f);
        public static readonly Constant PRICE_INCREASE_FREE = new Constant(TypeSymbol.Float, "PRICE_INCREASE_FREE", 0f);
        public static readonly Constant PRICE_INCREASE_FIXED_10 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_FIXED_10", 1f);
        public static readonly Constant PRICE_INCREASE_FIXED_100 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_FIXED_100", 4f);
        public static readonly Constant PRICE_INCREASE_FIXED_1000 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_FIXED_1000", 7f);
        public static readonly Constant PRICE_INCREASE_FIXED_10000 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_FIXED_10000", 10f);
        public static readonly Constant PRICE_INCREASE_LINEAR_10 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_LINEAR_10", 2f);
        public static readonly Constant PRICE_INCREASE_LINEAR_100 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_LINEAR_100", 5f);
        public static readonly Constant PRICE_INCREASE_LINEAR_1000 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_LINEAR_1000", 8f);
        public static readonly Constant PRICE_INCREASE_DOUBLE_10 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_DOUBLE_10", 3f);
        public static readonly Constant PRICE_INCREASE_DOUBLE_100 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_DOUBLE_100", 6f);
        public static readonly Constant PRICE_INCREASE_DOUBLE_1000 = new Constant(TypeSymbol.Float, "PRICE_INCREASE_DOUBLE_1000", 9f);

        private static Constant[]? cache;
        public static IEnumerable<Constant> GetAll()
            => cache ??= typeof(Constants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null) as Constant)
            .Where(val => val is Constant)
            .Select(constant => constant!)
            .ToArray();

        public static IEnumerable<(string Name, IEnumerable<Constant> Values)> GetGroups()
        {
            Constant[] all = GetAll().ToArray();

            // TODO: better way to do this
            return
            [
                get("TOUCH_STATE"),
                get("TOUCH_FINGER"),
                get("BUTTON_TYPE"),
                get("TOUCH_STATE"),
                get("JOYSTICK_TYPE"),
                get("SOUND"),
                get("RANKING"),
                get("MAX_ITEMS"),
                get("PRICE_INCREASE"),
            ];

            (string, IEnumerable<Constant>) get(string name)
            {
                return (name, all
                    .Where(con => con.Name.StartsWith(name)));
            }
        }
    }
}
