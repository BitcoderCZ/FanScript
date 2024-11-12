using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
    private static class Game
    {
        [FunctionDoc(
            ParameterInfos = [
                """
                Time to win (in frames).
                """
            ])]
        public static readonly FunctionSymbol Win
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "win",
                [
                    new ParameterSymbol("DELAY", Modifiers.Constant, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.Win, constantTypes: [typeof(byte)]));

        [FunctionDoc(
            ParameterInfos = [
                """
                Time to lose (in frames).
                """
            ])]
        public static readonly FunctionSymbol Lose
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "lose",
                [
                    new ParameterSymbol("DELAY", Modifiers.Constant, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.Lose, constantTypes: [typeof(byte)]));

        [FunctionDoc(
            Info = """
            Sets the <link type="param">score</> and/or <link type="param">coins</>.
            """,
            ParameterInfos = [
                """
                The new score, if <link type="param">RANKING</> is <link type="con_value">RANKING_TIME_FASTEST</> or <link type="con_value">RANKING_TIME_LONGEST</> time is specified in frames (60 - 1s).
                """,
                """
                The new amount of coins.
                """,
                """
                How players are ranked, one of <link type="con">RANKING</>.
                """
            ])]
        public static readonly FunctionSymbol SetScore
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "setScore",
                [
                    new ParameterSymbol("score", TypeSymbol.Float),
                    new ParameterSymbol("coins", TypeSymbol.Float),
                    new ParameterSymbol("RANKING", Modifiers.Constant, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.SetScore, constantTypes: [typeof(byte)]));

        [FunctionDoc(
            Info = """
            Sets the <link type="param">position</>, <link type="param">rotation</>, <link type="param">range</> and mode of the camera.
            """,
            ParameterInfos = [
                """
                The new position of the camera.
                """,
                """
                The new rotation of the camera.
                """,
                """
                <list>
                <item>If in orthographic (isometric) mode, determines how wide the view frustum is.</>
                <item>If in perspective mode specifies half of the field of view.</>
                </>
                """,
                """
                If true, the camera will be in perspective mode, otherwise it will be in orthographic mode.
                """
            ])]
        public static readonly FunctionSymbol SetCamera
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "setCamera",
                [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                    new ParameterSymbol("range", TypeSymbol.Float),
                    new ParameterSymbol("PERSPECTIVE", Modifiers.Constant, TypeSymbol.Bool),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.SetCamera, constantTypes: [typeof(byte)]));

        [FunctionDoc(
            Info = """
            Sets the direction of light.
            """,
            ParameterInfos = [
                """
                Currently unused.
                """,
                """
                The direction of light.
                """
            ],
            Remarks = [
                """
                If <link type="param">rotation</> is NaN (0 / 0), inf (1 / 0) or -inf (-1 / 0) there will be no shadows.
                """
            ])]
        public static readonly FunctionSymbol SetLight
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "setLight",
                [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.SetLight));

        [FunctionDoc(
            Info = """
            Gets the size of the screen.
            """,
            ParameterInfos = [
                """
                Width of the screen (in pixels).
                """,
                """
                Height of the screen (in pixels).
                """
            ])]
        public static readonly FunctionSymbol GetScreenSize
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "getScreenSize",
                [
                    new ParameterSymbol("width", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("height", Modifiers.Out, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 2, Blocks.Game.ScreenSize));

        [FunctionDoc(
            NameOverwrite = "GetScreenSize",
            Info = """
            Size of the screen in pixels, Width/Height - X/Y.
            """)]
        public static readonly FunctionSymbol GetScreenSize2
           = new BuiltinFunctionSymbol(GameNamespace, "getScreenSize", [], TypeSymbol.Vector3, (call, context) =>
           {
               Block make = context.AddBlock(Blocks.Math.Make_Vector);

               using (context.ExpressionBlock())
               {
                   Block ss = context.AddBlock(Blocks.Game.ScreenSize);

                   context.Connect(BasicEmitStore.COut(ss, ss.Type.Terminals["Width"]), BasicEmitStore.CIn(make, make.Type.Terminals["X"]));
                   context.Connect(BasicEmitStore.COut(ss, ss.Type.Terminals["Height"]), BasicEmitStore.CIn(make, make.Type.Terminals["Y"]));
               }

               return BasicEmitStore.COut(make, make.Type.Terminals["Vector"]);
           });

        [FunctionDoc(
            Info = """
            Gets the phone's current acceleration.
            """,
            ReturnValueInfo = """
            The acceleration, can be used to determine the phone's tilt.
            """,
            Examples = """
            <codeblock lang="fcs">
            // the acceleration can be smoothed out like this:
            vec3 smooth
            smooth += (getAccelerometer() - smooth) * 0.1
            </>
            """)]
        public static readonly FunctionSymbol GetAccelerometer
            = new BuiltinFunctionSymbol(GameNamespace, "getAccelerometer", [], TypeSymbol.Vector3, (call, context) => EmitX1(call, context, Blocks.Game.Accelerometer));

        [FunctionDoc(
            Info = """
            Gets the current frame.
            """,
            ReturnValueInfo = """
            The current frame.
            """,
            Remarks = [
                """
                Starts at 0, increases by 1 every frame.
                """,
                """
                Fancade runs at 60 frames per second.
                """
            ])]
        public static readonly FunctionSymbol GetCurrentFrame
            = new BuiltinFunctionSymbol(GameNamespace, "getCurrentFrame", [], TypeSymbol.Float, (call, context) => EmitX1(call, context, Blocks.Game.CurrentFrame));

        [FunctionDoc(
            Info = """
            Creates a section in the shop, calls to <link type="func">menuItem;float;obj;string;float;float</> after this will create items in this section.
            """,
            ParameterInfos = [
                """
                The name of this section, will be shown as a header above the items.
                """
            ],
            Related = [
                """
                <link type="func">menuItem;float;obj;string;float;float</>
                """
            ])]
        public static readonly FunctionSymbol ShopSection
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "shopSection",
                [
                    new ParameterSymbol("NAME", Modifiers.Constant, TypeSymbol.String),
                ],
                TypeSymbol.Void,
                (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                    if (constants is null)
                    {
                        return NopEmitStore.Instance;
                    }

                    Block block = context.AddBlock(Blocks.Game.MenuItem);

                    context.SetBlockValue(block, 0, constants[0] ?? string.Empty);

                    return new BasicEmitStore(block);
                });

        [FunctionDoc(
            Info = """
            Adds an item to the shop. Info about the shop can be found <link type="url">here;https://www.fancade.com/wiki/script/how-to-use-the-shop-system</>.
            """,
            ParameterInfos = [
                """
                Which variable to store the value of times bought in, should have <link type="mod">saved</> modifier.
                """,
                """
                Which object to display for the item.
                """,
                """
                Name of the item.
                """,
                """
                Maximum number of times the item can be bought, can be 2-100 or one of <link type="con">MAX_ITEMS</>.
                """,
                """
                Specifies what the initial price is and how it increases, one of <link type="con">PRICE_INCREASE</>.
                """
            ],
            Related = [
                """
                <link type="func">shopSection;string</>
                """
            ])]
        public static readonly FunctionSymbol MenuItem
            = new BuiltinFunctionSymbol(
                GameNamespace,
                "menuItem",
                [
                    new ParameterSymbol("variable", Modifiers.Ref, TypeSymbol.Float),
                    new ParameterSymbol("picture", TypeSymbol.Object),
                    new ParameterSymbol("NAME", Modifiers.Constant, TypeSymbol.String),
                    new ParameterSymbol("MAX_ITEMS", Modifiers.Constant, TypeSymbol.Float),
                    new ParameterSymbol("PRICE_INCREASE", Modifiers.Constant, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Game.MenuItem, constantTypes: [typeof(string), typeof(byte), typeof(byte)]));

        private static readonly Namespace GameNamespace = BuiltinNamespace + "game";
    }
}
