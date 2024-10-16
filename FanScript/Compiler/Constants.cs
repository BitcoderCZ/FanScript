using FanScript.Compiler.Symbols;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler
{
    public record ConstantGroup(TypeSymbol Type, string Name, Constant[] Values)
    {
    }
    public record Constant(string Name, object Value)
    {
    }

    public static class Constants
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
        public static readonly ConstantGroup BLOCK = new(TypeSymbol.Float, "BLOCK",
        [
            new Constant("AIR", 0f),
            new Constant("STONE_BLOCK", 1f),
            new Constant("BRICKS", 2f),
            new Constant("GRASS", 3f),
            new Constant("GRASS2", 4f),
            new Constant("DIRT", 5f),
            new Constant("WOOD_X", 6f),
            new Constant("WOOD_Z", 7f),
            new Constant("WOOD_Y", 8f),
            new Constant("FOLDER_UNKNOWN", 382f),
            new Constant("SWIPE_CHICK", 383f),
            new Constant("DIRT_SLAB", 384f),
            new Constant("STEEl", 385f),
            new Constant("ARCH", 386f),
            new Constant("BOX", 388f),
            new Constant("GOAL", 389f),
            new Constant("FOLDER_EMPTY", 415f),
            new Constant("FOLDER_LOCKED", 416f),
            new Constant("PHYSICS_BOX", 425f),
            new Constant("PHYSICS_SPHERE", 426f),
            new Constant("TILT_BALL", 427f),
            new Constant("PASS_THROUGH", 448f),
            new Constant("DIRT2", 493f),
            new Constant("FLOWERS", 494f),
            new Constant("FOLIAGE", 495f),
            new Constant("FOLIAGE2", 496f),
            new Constant("FOLIAGE_TOP", 497f),
            new Constant("FOLIAGE_BOTTOM", 498f),
            new Constant("FOLIAGE_SLAB", 499f),
            new Constant("SHRUB", 500f),
            new Constant("STONE", 501f),
            new Constant("STONE2", 502f),
            new Constant("STONE_TOP", 503f),
            new Constant("STONE_BOTTOM", 504f),
            new Constant("STONE_SLAB", 505f),
            new Constant("STONE_PILLAR", 506f),
            new Constant("STONE_HALF", 507f),
            new Constant("WOOD_HALF_BOTTOM_X", 508f),
            new Constant("WOOD_HALF_BOTTOM_Z", 509f),
            new Constant("WOOD_HALF_TOP_X", 510f),
            new Constant("WOOD_HALF_TOP_Z", 511f),
            new Constant("STICK_X", 512f),
            new Constant("STICK_Y", 513f),
            new Constant("STICK_Z", 514f),
            new Constant("STICK_PX_PZ", 515f),
            new Constant("STICK_NX_PZ", 516f),
            new Constant("STICK_NX_NZ", 517f),
            new Constant("STICK_PX_NZ", 518f),
            new Constant("STICK_PX_PY", 519f),
            new Constant("STICK_PY_PZ", 520f),
            new Constant("STICK_NX_PY", 521f),
            new Constant("STICK_PY_NZ", 522f),
            new Constant("STICK_PX_NY", 523f),
            new Constant("STICK_NY_PZ", 524f),
            new Constant("STICK_NX_NY", 525f),
            new Constant("STICK_NY_NZ", 526f),
            new Constant("WHEEL", 534f),
            new Constant("DASH_CAT", 535f),
            new Constant("MARKER", 536f),
            new Constant("TAP_DINO", 537f),
            new Constant("RED_DINO", 538f),
            new Constant("BUTTERFLY", 539f),
            new Constant("SCRIPT_BLOCK", 545f),
            new Constant("SPHERE", 546f),
            new Constant("SLATE", 547f),
            new Constant("SLATE2", 548f),
            new Constant("SLATE_NE", 549f),
            new Constant("SLATE_NW", 550f),
            new Constant("SLATE_SE", 551f),
            new Constant("SLATE_SW", 552f),
            new Constant("SLATE_TOP", 553f),
            new Constant("SLATE_BOTTOM", 554f),
            new Constant("PARTICLE", 556f),
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
