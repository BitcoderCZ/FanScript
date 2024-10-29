using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Raw;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Emit.Utils;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]
namespace FanScript.Compiler.Symbols
{

    internal static class BuiltinFunctions
    {
        private static Namespace builtinNamespace = new Namespace("builtin");

        public static readonly IReadOnlyDictionary<FunctionSymbol, FunctionDocAttribute> FunctionToDoc;

        static BuiltinFunctions()
        {
            FunctionToDoc = typeof(BuiltinFunctions)
                .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type) => type
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(FunctionSymbol))
                    .Select(f =>
                    {
                        FunctionSymbol func = (FunctionSymbol)f.GetValue(null)!;
                        FunctionDocAttribute? attrib = f.GetCustomAttribute<FunctionDocAttribute>();

                        if (attrib is null)
                            throw new Exception($"Field \"{f.Name}\" is missing {nameof(FunctionDocAttribute)}");

                        return (func, attrib);
                    })
                )
                .Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(FunctionSymbol))
                    .Select(f =>
                    {
                        FunctionSymbol func = (FunctionSymbol)f.GetValue(null)!;
                        FunctionDocAttribute? attrib = f.GetCustomAttribute<FunctionDocAttribute>();

                        if (attrib is null)
                            throw new Exception($"Field \"{f.Name}\" is missing {nameof(FunctionDocAttribute)}");

                        return (func, attrib);
                    })
                )
                .ToDictionary()
                .AsReadOnly();
        }

        #region Helper functions
        // (A) - active block (has before and after), num - numb inputs, num - number outputs
        private static EmitStore emitAX0(BoundCallExpression call, IEmitContext context, BlockDef blockDef, int argumentOffset = 0, Type[]? constantTypes = null)
        {
            constantTypes ??= [];

            Block block = context.AddBlock(blockDef);

            int argLength = call.Arguments.Length - constantTypes.Length;
            Debug.Assert(argLength >= 0);

            if (argLength != 0)
                using (context.ExpressionBlock())
                {
                    for (int i = 0; i < argLength; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.TerminalArray[argLength - i + argumentOffset]));
                }

            if (constantTypes.Length != 0)
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(^constantTypes.Length..), true);

                if (constants is null)
                    return NopEmitStore.Instance;

                for (int i = 0; i < constantTypes.Length; i++)
                {
                    object? val = constants[i];
                    Type type = constantTypes[i];

                    if (val is null)
                    {
                        if (type == typeof(string))
                            val = string.Empty;
                        else
                        {
                            Debug.Assert(type.IsValueType);
#pragma warning disable IL2062 // The parameter of method has a DynamicallyAccessedMembersAttribute, but the value passed to it can not be statically analyzed.
                            val = RuntimeHelpers.GetUninitializedObject(type);
#pragma warning restore IL2062
                        }
                    }
                    else
                        val = Convert.ChangeType(val, type);

                    context.SetBlockValue(block, i, val);
                }
            }

            return new BasicEmitStore(block);
        }
        private static EmitStore emitAXX(BoundCallExpression call, IEmitContext context, int numbReturnArgs, BlockDef blockDef)
        {
            if (numbReturnArgs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
            else if (numbReturnArgs > call.Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));

            int retArgStart = call.Arguments.Length - numbReturnArgs;

            Block? block = context.AddBlock(blockDef);

            EmitConnector connector = new EmitConnector(context.Connect);
            connector.Add(new BasicEmitStore(block));

            if (retArgStart != 0)
                using (context.ExpressionBlock())
                {
                    for (int i = 0; i < retArgStart; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.TerminalArray[retArgStart - i + numbReturnArgs]));
                }

            using (context.StatementBlock())
            {
                for (int i = retArgStart; i < call.Arguments.Length; i++)
                {
                    VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

                    EmitStore varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.TerminalArray[call.Arguments.Length - i]));

                    connector.Add(varStore);
                }
            }

            return connector.Store;
        }
        private static EmitStore emitXX(BoundCallExpression call, IEmitContext context, int numbReturnArgs, BlockDef blockDef)
        {
            if (numbReturnArgs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
            else if (numbReturnArgs > call.Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));

            int retArgStart = call.Arguments.Length - numbReturnArgs;

            EmitConnector connector = new EmitConnector(context.Connect);

            Block? block = null;

            for (int i = retArgStart; i < call.Arguments.Length; i++)
            {
                VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

                EmitStore varStore;

                if (block is null)
                {
                    varStore = context.EmitSetVariable(variable, () =>
                    {
                        using (context.ExpressionBlock())
                        {
                            block = context.AddBlock(blockDef);

                            if (retArgStart != 0)
                                using (context.ExpressionBlock())
                                {
                                    for (int i = 0; i < retArgStart; i++)
                                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.TerminalArray[(retArgStart - 1) - i + numbReturnArgs]));
                                }
                        }

                        return BasicEmitStore.COut(block, block.Type.TerminalArray[(call.Arguments.Length - 1) - i]);
                    });
                }
                else
                    varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.TerminalArray[(call.Arguments.Length - 1) - i]));

                connector.Add(varStore);
            }

            return connector.Store;
        }
        private static EmitStore emitX1(BoundCallExpression call, IEmitContext context, BlockDef blockDef)
        {
            Block block = context.AddBlock(blockDef);

            if (call.Arguments.Length != 0)
                using (context.ExpressionBlock())
                {
                    for (int i = 0; i < call.Arguments.Length; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.TerminalArray[call.Arguments.Length - i]));
                }

            return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
        }
        #endregion

        private static class Game
        {
            private static Namespace gameNamespace = builtinNamespace + "game";

            [FunctionDoc(
                ParameterInfos = [
                    """
                    Time to win (in frames).
                    """
                ]
            )]
            public static readonly FunctionSymbol Win
                = new BuiltinFunctionSymbol(gameNamespace, "win", [
                    new ParameterSymbol("DELAY", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Win, constantTypes: [typeof(byte)]));

            [FunctionDoc(
                ParameterInfos = [
                    """
                    Time to lose (in frames).
                    """
                ]
            )]
            public static readonly FunctionSymbol Lose
                = new BuiltinFunctionSymbol(gameNamespace, "lose", [
                    new ParameterSymbol("DELAY", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Lose, constantTypes: [typeof(byte)]));

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
                ]
            )]
            public static readonly FunctionSymbol SetScore
                = new BuiltinFunctionSymbol(gameNamespace, "setScore", [
                    new ParameterSymbol("score", TypeSymbol.Float),
                    new ParameterSymbol("coins", TypeSymbol.Float),
                    new ParameterSymbol("RANKING", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetScore, constantTypes: [typeof(byte)]));

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
                ]
            )]
            public static readonly FunctionSymbol SetCamera
                = new BuiltinFunctionSymbol(gameNamespace, "setCamera", [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                    new ParameterSymbol("range", TypeSymbol.Float),
                    new ParameterSymbol("PERSPECTIVE", Modifiers.Constant, TypeSymbol.Bool),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetCamera, constantTypes: [typeof(byte)]));

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
                ]
            )]
            public static readonly FunctionSymbol SetLight
                = new BuiltinFunctionSymbol(gameNamespace, "setLight", [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetLight));

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
                ]
            )]
            public static readonly FunctionSymbol GetScreenSize
                = new BuiltinFunctionSymbol(gameNamespace, "getScreenSize", [
                    new ParameterSymbol("width", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("height", Modifiers.Out, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Game.ScreenSize));

            [FunctionDoc(
                NameOverwrite = "GetScreenSize",
                Info = """
                Size of the screen in pixels, Width/Height - X/Y.
                """
            )]
            public static readonly FunctionSymbol GetScreenSize2
               = new BuiltinFunctionSymbol(gameNamespace, "getScreenSize", [], TypeSymbol.Vector3, (call, context) =>
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
                """
            )]
            public static readonly FunctionSymbol GetAccelerometer
                = new BuiltinFunctionSymbol(gameNamespace, "getAccelerometer", [], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Game.Accelerometer));

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
                ]
            )]
            public static readonly FunctionSymbol GetCurrentFrame
                = new BuiltinFunctionSymbol(gameNamespace, "getCurrentFrame", [], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Game.CurrentFrame));

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
                ]
            )]
            public static readonly FunctionSymbol ShopSection
                = new BuiltinFunctionSymbol(gameNamespace, "shopSection", [
                    new ParameterSymbol("NAME", Modifiers.Constant, TypeSymbol.String),
                ], TypeSymbol.Void, (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                    if (constants is null)
                        return NopEmitStore.Instance;

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
                ]
            )]
            public static readonly FunctionSymbol MenuItem
                = new BuiltinFunctionSymbol(gameNamespace, "menuItem", [
                    new ParameterSymbol("variable", Modifiers.Ref, TypeSymbol.Float),
                    new ParameterSymbol("picture", TypeSymbol.Object),
                    new ParameterSymbol("NAME", Modifiers.Constant, TypeSymbol.String),
                    new ParameterSymbol("MAX_ITEMS", Modifiers.Constant, TypeSymbol.Float),
                    new ParameterSymbol("PRICE_INCREASE", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.MenuItem, constantTypes: [typeof(string), typeof(byte), typeof(byte)]));
        }

        private static class Objects
        {
            private static Namespace objectNamespace = builtinNamespace + "object";

            [FunctionDoc(
                Info = """
                Returns the object at <link type="param">POSITION</>.
                """,
                ReturnValueInfo = """
                The object at <link type="param">POSITION</>.
                """,
                ParameterInfos = [
                    """
                    Position of the object.
                    """
                ],
                Related = [
                    """
                    <link type="func">getObject;float;float;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetObject
              = new BuiltinFunctionSymbol(objectNamespace, "getObject", [
                  new ParameterSymbol("POSITION", Modifiers.Constant, TypeSymbol.Vector3),
              ], TypeSymbol.Object, (call, context) =>
              {
                  BoundConstant? constant = call.Arguments[0].ConstantValue;
                  if (constant is null)
                  {
                      context.Diagnostics.ReportValueMustBeConstant(call.Arguments[0].Syntax.Location);
                      return NopEmitStore.Instance;
                  }

                  Vector3I pos = (Vector3I)((Vector3F)constant.GetValueOrDefault(TypeSymbol.Vector3)); // unbox, then cast

                  if (context.Builder is not IConnectToBlocksBuilder)
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);

                      using (context.ExpressionBlock())
                          context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");

                      return NopEmitStore.Instance;
                  }

                  return new AbsoluteEmitStore(pos, null);
              }
              );

            [FunctionDoc(
                Info = """
                Returns the object at (<link type="param">X</>, <link type="param">Y</>, <link type="param">Z</>).
                """,
                ReturnValueInfo = """
                The object at (<link type="param">X</>, <link type="param">Y</>, <link type="param">Z</>).
                """,
                ParameterInfos = [
                    """
                    X position of the object.
                    """,
                    """
                    Y position of the object.
                    """,
                    """
                    Z position of the object.
                    """
                ],
                Related = [
                    """
                    <link type="func">getObject;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetObject2
              = new BuiltinFunctionSymbol(objectNamespace, "getObject", [
                  new ParameterSymbol("X", Modifiers.Constant, TypeSymbol.Float),
                  new ParameterSymbol("Y", Modifiers.Constant, TypeSymbol.Float),
                  new ParameterSymbol("Z", Modifiers.Constant, TypeSymbol.Float),
              ], TypeSymbol.Object, (call, context) =>
              {
                  object?[]? args = context.ValidateConstants(call.Arguments.AsMemory(), true);
                  if (args is null)
                      return NopEmitStore.Instance;

                  Vector3I pos = new Vector3I((int)((float?)args[0] ?? 0f), (int)((float?)args[1] ?? 0f), (int)((float?)args[2] ?? 0f)); // unbox, then cast

                  if (context.Builder is not IConnectToBlocksBuilder)
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);

                      using (context.ExpressionBlock())
                          context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");

                      return NopEmitStore.Instance;
                  }

                  return new AbsoluteEmitStore(pos, null);
              }
              );

            [FunctionDoc(
                Info = """
                Sets the position of <link type="param">object</>.
                """,
                Related = [
                    """
                    <link type="func">getPos;obj;vec3;rot</>
                    """,
                    """
                    <link type="func">setPos;obj;vec3;rot</>
                    """
                ]
            )]
            public static readonly FunctionSymbol SetPos
                = new BuiltinFunctionSymbol(objectNamespace, "setPos", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.SetPos, argumentOffset: 1))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the position and rotation of <link type="param">object</>.
                """,
                Related = [
                    """
                    <link type="func">getPos;obj;vec3;rot</>
                    """,
                    """
                    <link type="func">setPos;obj;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol SetPosWithRot
                = new BuiltinFunctionSymbol(objectNamespace, "setPos",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.SetPos))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Gets the <link type="param">position</> and <link type="param">rotation</> of <link type="param">object</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    <link type="param">object</>'s position.
                    """,
                    """
                    <link type="param">object</>'s rotation.
                    """
                ],
                Related = [
                    """
                    <link type="func">setPos;obj;vec3</>
                    """,
                    """
                    <link type="func">setPos;obj;vec3;rot</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetPos
                = new BuiltinFunctionSymbol(objectNamespace, "getPos", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", Modifiers.Out, TypeSymbol.Rotation),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Objects.GetPos))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Detects if an object intersects a line between <link type="param">from</> and <link type="param">to</>.
                """,
                ParameterInfos = [
                    """
                    From position of the ray.
                    """,
                    """
                    To position of the ray.
                    """,
                    """
                    If the ray hit an object.
                    """,
                    """
                    The position at which the ray intersected <link type="param">hitObj</>.
                    """,
                    """
                    The object that was hit.
                    """
                ],
                Remarks = [
                    """
                    Only detects the outside surface of a block. If it starts inside of a block, the block won't be detected.
                    """,
                    """
                    Won't detect object created on the same frame as the raycast is performed.
                    """,
                    """
                    Won't detect objects without collion or script blocks.
                    """,
                    """
                    If the raycast hits the floor, <link type="param">hitObj</> will be equal to null.
                    """
                ]
            )]
            public static readonly FunctionSymbol Raycast
                = new BuiltinFunctionSymbol(objectNamespace, "raycast", [
                    new ParameterSymbol("from", TypeSymbol.Vector3),
                    new ParameterSymbol("to", TypeSymbol.Vector3),
                    new ParameterSymbol("didHit", Modifiers.Out, TypeSymbol.Bool),
                    new ParameterSymbol("hitPos", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("hitObj", Modifiers.Out, TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 3, Blocks.Objects.Raycast));

            [FunctionDoc(
                Info = """
                Gets the size of <link type="param">object</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    Distance from the center of <link type="param">object</> to the negative edge.
                    """,
                    """
                    Distance from the center of <link type="param">object</> to the positive edge.
                    """
                ],
                Remarks = [
                    """
                    Size is measured in blocks, not in voxels.
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                // to get the total size of the object, you can do this:
                object obj
                obj.getSize(out inline vec3 min, out inline vec3 max)
                vec3 totalSize = max - min
                </>
                """
            )]
            public static readonly FunctionSymbol GetSize
                = new BuiltinFunctionSymbol(objectNamespace, "getSize", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("min", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("max", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Objects.GetSize))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets if <link type="param">object</> is visible and has collision/physics.
                """,
                Remarks = [
                    """
                    When <link type="param">object</> is set to invisible, all constraints associated with it will be deleted.
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                // Here's how an object can be invisible, while also having physics:
                object obj
                obj.setVisible(true)
                on LateUpdate
                {
                    obj.setVisible(false)
                }
                </>
                """
            )]
            public static readonly FunctionSymbol SetVisible
                = new BuiltinFunctionSymbol(objectNamespace, "setVisible", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("visible", TypeSymbol.Bool),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.SetVisible))
                {
                    IsMethod = true,
                };
            [FunctionDoc(
                Info = """
                Creates a copy of <link type="param">object</>.
                """,
                ParameterInfos = [
                    """
                    The object to copy.
                    """,
                    """
                    The copy of <link type="param">object</>.
                    """
                ],
                Remarks = [
                    """
                    Scripts inside <link type="param">object</> do not get copied inside of <link type="param">copy</>.
                    """
                ],
                Related = [
                    """
                    <link type="func">destroy;obj</>
                    """
                ]
            )]
            public static readonly FunctionSymbol Clone
                = new BuiltinFunctionSymbol(objectNamespace, "clone", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("copy", Modifiers.Out, TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitAXX(call, context, 1, Blocks.Objects.CreateObject))
                {
                    IsMethod = true,
                };
            [FunctionDoc(
                Info = """
                Destroys <link type="param">object</>.
                """,
                ParameterInfos = [
                    """
                    The object to destroy.
                    """
                ],
                Remarks = [
                    """
                    **Only destroys blocks created by <link type="func">clone;obj;obj</>**.
                    """
                ],
                Related = [
                    """
                    <link type="func">clone;obj;obj</>
                    """
                ]
            )]
            public static readonly FunctionSymbol Destroy
                = new BuiltinFunctionSymbol(objectNamespace, "destroy", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.DestroyObject))
                {
                    IsMethod = true,
                };
        }

        private static class Sound
        {
            private static Namespace soundNamespace = builtinNamespace + "sound";

            [FunctionDoc(
                Info = """
                Plays the <link type="param">SOUND</>.
                """,
                ParameterInfos = [
                    """
                    Volume of the sound (0 - 1).
                    """,
                    """
                    Pitch of the sound (0 - 4).
                    """,
                    """
                    The channel at which the sound is playing (0 - 9, or -1 if all other channels are used).
                    """,
                    """
                    If the sound should loop.
                    """,
                    """
                    Which sound to play, one of <link type="con">SOUND</>.
                    """
                ],
                Related = [
                    """
                    <link type="func">stopSound;float</>
                    """,
                    """
                    <link type="func">setVolumePitch;float;float;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol PlaySound
                = new BuiltinFunctionSymbol(soundNamespace, "playSound",
                [
                    new ParameterSymbol("volume", TypeSymbol.Float),
                    new ParameterSymbol("pitch", TypeSymbol.Float),
                    new ParameterSymbol("channel", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("LOOP", Modifiers.Constant, TypeSymbol.Bool),
                    new ParameterSymbol("SOUND", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) =>
              {
                  object?[]? values = context.ValidateConstants(call.Arguments.AsMemory(^2..), true);
                  if (values is null)
                      return NopEmitStore.Instance;

                  Block playSound = context.AddBlock(Blocks.Sound.PlaySound);

                  context.SetBlockValue(playSound, 0, (byte)(((bool?)values[0] ?? false) ? 1 : 0)); // loop
                  context.SetBlockValue(playSound, 1, (ushort)((float?)values[1] ?? 0f)); // sound

                  using (context.ExpressionBlock())
                  {
                      EmitStore volume = context.EmitExpression(call.Arguments[0]);
                      EmitStore pitch = context.EmitExpression(call.Arguments[1]);

                      context.Connect(volume, BasicEmitStore.CIn(playSound, playSound.Type.Terminals["Volume"]));
                      context.Connect(pitch, BasicEmitStore.CIn(playSound, playSound.Type.Terminals["Pitch"]));
                  }

                  EmitStore varStore;
                  using (context.StatementBlock())
                  {
                      VariableSymbol variable = ((BoundVariableExpression)call.Arguments[2]).Variable;

                      varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(playSound, playSound.Type.Terminals["Channel"]));

                      context.Connect(BasicEmitStore.COut(playSound), varStore);
                  }

                  return new MultiEmitStore(BasicEmitStore.CIn(playSound), varStore is NopEmitStore ? BasicEmitStore.COut(playSound) : varStore);
              });

            [FunctionDoc(
                Info = """
                Stops the sound playing at <link type="param">channel</>.
                """,
                ParameterInfos = [
                    """
                    The channel from <link type="func">playSound;float;float;float;bool;float</>.
                    """
                ],
                Related = [
                    """
                    <link type="func">playSound;float;float;float;bool;float</>
                    """,
                    """
                    <link type="func">setVolumePitch;float;float;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol StopSound
                 = new BuiltinFunctionSymbol(soundNamespace, "stopSound",
                 [
                    new ParameterSymbol("channel", TypeSymbol.Float),
                 ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Sound.StopSound));

            [FunctionDoc(
                Info = """
                Sets the <link type="param">volume</> and <link type="param">pitch</> of the sound playing at <link type="param">channel</>.
                """,
                ParameterInfos = [
                    """
                    The channel from <link type="func">playSound;float;float;float;bool;float</>.
                    """,
                    """
                    The new volume (0 - 1).
                    """,
                    """
                    The new pitch (0 - 4).
                    """
                ],
                Related = [
                    """
                    <link type="func">playSound;float;float;float;bool;float</>
                    """,
                    """
                    <link type="func">stopSound;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol SetVolumePitch
                 = new BuiltinFunctionSymbol(soundNamespace, "setVolumePitch",
                 [
                    new ParameterSymbol("channel", TypeSymbol.Float),
                     new ParameterSymbol("volume", TypeSymbol.Float),
                     new ParameterSymbol("pitch", TypeSymbol.Float),
                 ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Sound.VolumePitch));
        }

        private static class Physics
        {
            private static Namespace physicsNamespace = builtinNamespace + "physics";

            [FunctionDoc(
                Info = """
                Adds <link type="param">force</> and/or <link type="param">torque</> to <link type="param">object</>.
                """,
                ParameterInfos = [
                    """
                    The object that the force will be applied to.
                    """,
                    """
                    The force to apply to <link type="param">object</>.
                    """,
                    """
                    Where on <link type="param">object</> should <link type="param">force</> be applied at (center of mass by default).
                    """,
                    """
                    The rotational force to apply to <link type="param">object</>.
                    """
                ],
                Related = [
                    """
                    <link type="func">setVelocity;obj;vec3;vec3</>
                    """,
                    """
                    <link type="func">getVelocity;obj;vec3;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol AddForce
                = new BuiltinFunctionSymbol(physicsNamespace, "addForce",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("force", TypeSymbol.Vector3),
                    new ParameterSymbol("applyAt", TypeSymbol.Vector3),
                    new ParameterSymbol("torque", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AddForce))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Gets the <link type="param">object</>'s velocity.
                """,
                ParameterInfos = [
                    null,
                    """
                    Linear velocity of <link type="param">object</> (units/second).
                    """,
                    """
                    Angular velocity of <link type="param">object</> (degrees/second).
                    """
                ],
                Related = [
                    """
                    <link type="func">setVelocity;obj;vec3;vec3</>
                    """,
                    """
                    <link type="func">addForce;obj;vec3;vec3;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetVelocity
                = new BuiltinFunctionSymbol(physicsNamespace, "getVelocity",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("velocity", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("spin", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Physics.GetVelocity))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the linear and angular velocity of <link type="param">object</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    The linear velocity (units/second).
                    """,
                    """
                    The angular velocity (degrees/second).
                    """
                ],
                Related = [
                    """
                    <link type="func">getVelocity;obj;vec3;vec3</>
                    """,
                    """
                    <link type="func">addForce;obj;vec3;vec3;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol SetVelocity
                = new BuiltinFunctionSymbol(physicsNamespace, "setVelocity",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("velocity", TypeSymbol.Vector3),
                    new ParameterSymbol("spin", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetVelocity))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Restricts <link type="param">object</>'s movement, forces applied to <link type="param">object</> are multiplied by <link type="param">position</> and <link type="param">rotation</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    The movement multiplier.
                    """,
                    """
                    The rotation multiplier.
                    """
                ],
                Remarks = [
                    """
                    Negative numbers reverse physics and numbers bigger than 1 increase them.
                    """
                ]
            )]
            public static readonly FunctionSymbol SetLocked
                = new BuiltinFunctionSymbol(physicsNamespace, "setLocked",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetLocked))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the mass of <link type="param">object</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    The new mass of <link type="param">object</> (the default values is determined by the volume of <link type="param">object</>'s collider).
                    """
                ]
            )]
            public static readonly FunctionSymbol SetMass
                = new BuiltinFunctionSymbol(physicsNamespace, "setMass",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("mass", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetMass))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the friction of <link type="param">object</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    How much friction to apply to <link type="param">object</> when colliding with other objects (0.5 by default).
                    """
                ]
            )]
            public static readonly FunctionSymbol SetFriction
                = new BuiltinFunctionSymbol(physicsNamespace, "setFriction",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("friction", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetFriction))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets how bouncy <link type="param">object</> is (how much momentum it retains after each bounce).
                """,
                ParameterInfos = [
                    null,
                    """
                    How much momentum <link type="param">object</> retains after each bounce (0 - 1, if higher, <link type="param">object</>'s velocity will increase after each jump).
                    """
                ]
            )]
            public static readonly FunctionSymbol SetBounciness
                = new BuiltinFunctionSymbol(physicsNamespace, "setBounciness",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("bounciness", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetBounciness))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the direction (and magnitude) in which any physics objects falls.
                """,
                ParameterInfos = [
                    """
                    Default is (0, -9.8, 0).
                    """
                ]
            )]
            public static readonly FunctionSymbol SetGravity
                = new BuiltinFunctionSymbol(physicsNamespace, "setGravity",
                [
                    new ParameterSymbol("gravity", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetGravity));

            [FunctionDoc(
                Info = """
                Creates a <link type="param">constraint</> (an invisible connection (rod) between 2 objects) between <link type="param">part</> and <link type="param">base</>.
                """,
                ParameterInfos = [
                    """
                    The object that will be glued on.
                    """,
                    """
                    The object that will be glued on the <link type="param">base</>.
                    """,
                    """
                    The other end of the constraint rod.
                    """,
                    """
                    The created constraint.
                    """
                ],
                Related = [
                    """
                    <link type="func">linearLimits;constr;vec3;vec3</>
                    """,
                    """
                    <link type="func">angularLimits;constr;vec3;vec3</>
                    """,
                    """
                    <link type="func">linearSpring;constr;vec3;vec3</>
                    """,
                    """
                    <link type="func">angularSpring;constr;vec3;vec3</>
                    """,
                    """
                    <link type="func">linearMotor;constr;vec3;vec3</>
                    """,
                    """
                    <link type="func">angularMotor;constr;vec3;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol AddConstraint
                = new BuiltinFunctionSymbol(physicsNamespace, "addConstraint",
                [
                    new ParameterSymbol("base", TypeSymbol.Object),
                    new ParameterSymbol("part", TypeSymbol.Object),
                    new ParameterSymbol("pivot", TypeSymbol.Vector3),
                    new ParameterSymbol("constraint", Modifiers.Out, TypeSymbol.Constraint),
                ], TypeSymbol.Void, (call, context) => emitAXX(call, context, 1, Blocks.Physics.AddConstraint))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the linear limits of <link type="param">constraint</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    The lower limit.
                    """,
                    """
                    The upper limit.
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol LinearLimits
                = new BuiltinFunctionSymbol(physicsNamespace, "linearLimits",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("lower", TypeSymbol.Vector3),
                    new ParameterSymbol("upper", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearLimits))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Sets the angular limits of <link type="param">constraint</>.
                """,
                ParameterInfos = [
                    null,
                    """
                    The lower angular limit (in degrees).
                    """,
                    """
                    The upper angular limit (in degrees).
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol AngularLimits
                = new BuiltinFunctionSymbol(physicsNamespace, "angularLimits",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("lower", TypeSymbol.Vector3),
                    new ParameterSymbol("upper", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AngularLimits))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Makes the constraint springy, <link type="func">linearLimits;constr;vec3;vec3</> must be called before for linear spring to work.
                """,
                ParameterInfos = [
                    null,
                    """
                    How stiff the sping will be.
                    """,
                    """
                    How much damping (drag) to apply.
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol LinearSpring
                = new BuiltinFunctionSymbol(physicsNamespace, "linearSpring",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("stiffness", TypeSymbol.Vector3),
                    new ParameterSymbol("damping", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearSpring))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Makes the constraint springy, <link type="func">angularLimits;constr;vec3;vec3</> must be called before for angular spring to work.
                """,
                ParameterInfos = [
                    null,
                    """
                    How stiff the sping will be.
                    """,
                    """
                    How much damping (drag) to apply.
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol AngularSpring
                = new BuiltinFunctionSymbol(physicsNamespace, "angularSpring",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("stiffness", TypeSymbol.Vector3),
                    new ParameterSymbol("damping", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AngularSpring))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Makes <link type="param">constraint</> move.
                """,
                ParameterInfos = [
                    null,
                    """
                    The speed at which to move at.
                    """,
                    """
                    How much force to apply.
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol LinearMotor
                = new BuiltinFunctionSymbol(physicsNamespace, "linearMotor",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("speed", TypeSymbol.Vector3),
                    new ParameterSymbol("force", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearMotor))
                {
                    IsMethod = true,
                };

            [FunctionDoc(
                Info = """
                Makes <link type="param">constraint</> rotate.
                """,
                ParameterInfos = [
                    null,
                    """
                    The speed at which to rotate at.
                    """,
                    """
                    How much force to apply.
                    """
                ],
                Related = [
                    """
                    <link type="func">addConstraint;obj;obj;vec3;constr</>
                    """
                ]
            )]
            public static readonly FunctionSymbol AngularMotor
                = new BuiltinFunctionSymbol(physicsNamespace, "angularMotor",
                [
                    new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("speed", TypeSymbol.Vector3),
                    new ParameterSymbol("force", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AngularMotor))
                {
                    IsMethod = true,
                };
        }

        private static class Control
        {
            private static Namespace controlNamespace = builtinNamespace + "control";

            [FunctionDoc(
                Info = """
                Creates a joystick on screen and outputs the direction in which it is held.
                """,
                ParameterInfos = [
                    """
                    The direction which the joystick is held.
                    """,
                    """
                    One of <link type="con">JOYSTICK_TYPE</>.
                    """
                ]
            )]
            public static readonly FunctionSymbol Joystick
                = new BuiltinFunctionSymbol(controlNamespace, "joystick",
                [
                    new ParameterSymbol("joyDir", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("JOYSTICK_TYPE", Modifiers.Constant, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) =>
                {
                    object?[]? values = context.ValidateConstants(call.Arguments.AsMemory(Range.StartAt(1)), true);
                    if (values is null)
                        return NopEmitStore.Instance;

                    Block joystick = context.AddBlock(Blocks.Control.Joystick);

                    context.SetBlockValue(joystick, 0, (byte)((float?)values[0] ?? 0f)); // unbox, then cast

                    EmitStore varStore;
                    using (context.StatementBlock())
                    {
                        VariableSymbol variable = ((BoundVariableExpression)call.Arguments[0]).Variable;

                        varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(joystick, joystick.Type.Terminals["Joy Dir"]));

                        context.Connect(BasicEmitStore.COut(joystick), varStore);
                    }

                    return new MultiEmitStore(BasicEmitStore.CIn(joystick), varStore is NopEmitStore ? BasicEmitStore.COut(joystick) : varStore);
                });
        }

        private static class Math
        {
            private static Namespace mathNamespace = builtinNamespace + "math";

            [FunctionDoc(
                Info = """
                Raises <link type="param">base</> to <link type="param">exponent</>.
                """,
                ReturnValueInfo = """
                <link type="param">base</> raised to <link type="param">exponent</>.
                """
            )]
            public static readonly FunctionSymbol Pow
                = new BuiltinFunctionSymbol(mathNamespace, "pow",
                [
                    new ParameterSymbol("base", TypeSymbol.Float),
                    new ParameterSymbol("exponent", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Power));

            [FunctionDoc(
                Info = """
                Returns a random float between <link type="param">min</> and <link type="param">max</>.
                """,
                ReturnValueInfo = """
                The randomly selected number.
                """,
                ParameterInfos = [
                    """
                    The minimum number (inclusive).
                    """,
                    """
                    The maximum number (exclusive).
                    """
                ],
                Remarks = [
                    """
                    Do not use in inline variables, may return the same number.
                    """
                ]
            )]
            public static readonly FunctionSymbol Random
                = new BuiltinFunctionSymbol(mathNamespace, "random",
                [
                    new ParameterSymbol("min", TypeSymbol.Float),
                    new ParameterSymbol("max", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Random));

            [FunctionDoc(
                Info = """
                Sets the seed used by <link type="func">random;float;float</>.
                """,
                ParameterInfos = [
                    """
                    The new random seed.
                    """,
                ]
            )]
            public static readonly FunctionSymbol RandomSeed
                = new BuiltinFunctionSymbol(mathNamespace, "setRandomSeed",
                [
                    new ParameterSymbol("seed", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Math.RandomSeed));

            [FunctionDoc(
                Info = """
                Returns the smaller of 2 numbers.
                """,
                ReturnValueInfo = """
                Returns either <link type="param">num1</> or <link type="param">num2</>, depending on which one is smaller.
                """
            )]
            public static readonly FunctionSymbol Min
                = new BuiltinFunctionSymbol(mathNamespace, "min",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Min));

            [FunctionDoc(
                Info = """
                Returns the larger of 2 numbers.
                """,
                ReturnValueInfo = """
                Returns either <link type="param">num1</> or <link type="param">num2</>, depending on which one is larger.
                """
            )]
            public static readonly FunctionSymbol Max
                = new BuiltinFunctionSymbol(mathNamespace, "max",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Max));

            [FunctionDoc(
                Info = """
                Retuns <link type="url">sine;https://en.wikipedia.org/wiki/Sine_and_cosine</> of <link type="param">num</>.
                """,
                ReturnValueInfo = """
                Sine of <link type="param">num</>.
                """,
                ParameterInfos = [
                    """
                    Angle in degrees.
                    """,
                ],
                Related = [
                    """
                    <link type="func">cos;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol Sin
                = new BuiltinFunctionSymbol(mathNamespace, "sin",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Sin));

            [FunctionDoc(
                Info = """
                Retuns <link type="url">cosine;https://en.wikipedia.org/wiki/Sine_and_cosine</> of <link type="param">num</>.
                """,
                ReturnValueInfo = """
                Cos of <link type="param">num</>.
                """,
                ParameterInfos = [
                    """
                    Angle in degrees.
                    """,
                ],
                Related = [
                    """
                    <link type="func">sin;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol Cos
                = new BuiltinFunctionSymbol(mathNamespace, "cos",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Cos));

            [FunctionDoc(
                Info = """
                Returns <link type="param">num</> rounded to the nearest integer.
                """,
                ReturnValueInfo = """
                <link type="param">num</> rounded to the nearest integer.
                """
            )]
            public static readonly FunctionSymbol Round
                = new BuiltinFunctionSymbol(mathNamespace, "round",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Round));

            [FunctionDoc(
                Info = """
                Returns <link type="param">num</> rounded down.
                """,
                ReturnValueInfo = """
                <link type="param">num</> rounded down.
                """
            )]
            public static readonly FunctionSymbol Floor
                = new BuiltinFunctionSymbol(mathNamespace, "floor",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Floor));

            [FunctionDoc(
                Info = """
                Returns <link type="param">num</> rounded up.
                """,
                ReturnValueInfo = """
                <link type="param">num</> rounded up.
                """
            )]
            public static readonly FunctionSymbol Ceiling
                = new BuiltinFunctionSymbol(mathNamespace, "ceiling",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Ceiling));

            [FunctionDoc(
                Info = """
                Gets the absolute value of <link type="param">num</>.
                """,
                ReturnValueInfo = """
                Absolute value of <link type="param">num</> (if (<link type="param">num</> >= 0) <link type="param">num</> else -<link type="param">num</>).
                """
            )]
            public static readonly FunctionSymbol Abs
                = new BuiltinFunctionSymbol(mathNamespace, "abs",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Absolute));

            [FunctionDoc(
                Info = """
                Returns the <link type="url">logarithm;https://en.wikipedia.org/wiki/Logarithm</> of <link type="param">number</> to <link type="param">base</>.
                """
            )]
            public static readonly FunctionSymbol Log
                = new BuiltinFunctionSymbol(mathNamespace, "log",
                [
                    new ParameterSymbol("number", TypeSymbol.Float),
                    new ParameterSymbol("base", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Logarithm));

            [FunctionDoc(
                Info = """
                Returns a vector with the same direction as <link type="param">vector</>, but with the lenght of 1.
                """,
                ReturnValueInfo = """
                The normalized <link type="param">vector</>.
                """
            )]
            public static readonly FunctionSymbol Normalize
                = new BuiltinFunctionSymbol(mathNamespace, "normalize",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.Normalize));

            [FunctionDoc(
                Info = """
                Returns the <link type="url">dot product;https://en.wikipedia.org/wiki/Dot_product</> of <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ReturnValueInfo = """
                Dot product of <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ParameterInfos = [
                    """
                    The first vector.
                    """,
                    """
                    The second vector.
                    """
                ]
            )]
            public static readonly FunctionSymbol DotProduct
                = new BuiltinFunctionSymbol(mathNamespace, "dot",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.DotProduct));

            [FunctionDoc(
                Info = """
                Returns the <link type="url">cross product;https://en.wikipedia.org/wiki/Cross_product</> of <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ReturnValueInfo = """
                Cross product of <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ParameterInfos = [
                    """
                    The first vector.
                    """,
                    """
                    The second vector.
                    """
                ]
            )]
            public static readonly FunctionSymbol CrossProduct
                = new BuiltinFunctionSymbol(mathNamespace, "cross",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.CrossProduct));

            [FunctionDoc(
                Info = """
                Returns the distance between <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ReturnValueInfo = """
                The distance between <link type="param">vector1</> and <link type="param">vector2</>.
                """,
                ParameterInfos = [
                    """
                    The first vector.
                    """,
                    """
                    The second vector.
                    """
                ]
            )]
            public static readonly FunctionSymbol Distance
                = new BuiltinFunctionSymbol(mathNamespace, "dist",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Distance));

            [FunctionDoc(
                Info = """
                Linearly interpolates between <link type="param">from</> and <link type="param">to</>, depending on <link type="param">amount</>.
                """,
                ReturnValueInfo = """
                The value between <link type="param">from</> and <link type="param">to</>.
                """,
                ParameterInfos = [
                    """
                    The start value.
                    """,
                    """
                    The end value.
                    """,
                    """
                    How far between <link type="param">from</> and <link type="param">to</> to transition (0 - 1).
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                // ease out from "from" to "to"
                rot from
                rot to
                float speed
                on Play()
                {
                    from = rot(0, 0, 0)
                    to = rot(0, 90, 0)
                    speed = 0.05 // 5% between from and to
                }
                from = lerp(from, to, speed)
                </>
                """
            )]
            public static readonly FunctionSymbol Lerp
                = new BuiltinFunctionSymbol(mathNamespace, "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Rotation),
                    new ParameterSymbol("to", TypeSymbol.Rotation),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.Lerp));

            [FunctionDoc(
                NameOverwrite = "Lerp",
                Info = """
                Linearly interpolates between <link type="param">from</> and <link type="param">to</>, depending on <link type="param">amount</>.
                """,
                ReturnValueInfo = """
                The value between <link type="param">from</> and <link type="param">to</>.
                """,
                ParameterInfos = [
                    """
                    The start value.
                    """,
                    """
                    The end value.
                    """,
                    """
                    How far between <link type="param">from</> and <link type="param">to</> to transition (0 - 1).
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                // ease out from "from" to "to"
                float from
                float to
                float speed
                on Play()
                {
                    from = 0
                    to = 100
                    speed = 0.05 // 5% between from and to
                }
                from = lerp(from, to, speed)
                </>
                """
            )]
            public static readonly FunctionSymbol LerpFloat
                = new BuiltinFunctionSymbol(mathNamespace, "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Float),
                    new ParameterSymbol("to", TypeSymbol.Float),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

                    if (constants is not null)
                    {
                        float from = (constants[0] as float?) ?? 0f;
                        float to = (constants[1] as float?) ?? 0f;
                        float amount = (constants[2] as float?) ?? 0f;

                        Block numb = context.AddBlock(Blocks.Values.Number);
                        context.SetBlockValue(numb, 0, from + amount * (to - from));

                        return BasicEmitStore.COut(numb, numb.Type.Terminals["Number"]);
                    }

                    Block add = context.AddBlock(Blocks.Math.Add_Number);

                    using (context.ExpressionBlock())
                    {
                        context.Connect(context.EmitExpression(call.Arguments[0]), BasicEmitStore.CIn(add, add.Type.Terminals["Num1"]));

                        Block mult = context.AddBlock(Blocks.Math.Multiply_Number);

                        using (context.ExpressionBlock())
                        {
                            context.Connect(context.EmitExpression(call.Arguments[2]), BasicEmitStore.CIn(mult, mult.Type.Terminals["Num1"]));

                            Block sub = context.AddBlock(Blocks.Math.Subtract_Number);

                            using (context.ExpressionBlock())
                            {
                                context.Connect(context.EmitExpression(call.Arguments[1]), BasicEmitStore.CIn(sub, sub.Type.Terminals["Num1"]));
                                context.Connect(context.EmitExpression(call.Arguments[0]), BasicEmitStore.CIn(sub, sub.Type.Terminals["Num2"]));
                            }

                            context.Connect(BasicEmitStore.COut(sub, sub.Type.Terminals["Num1 - Num2"]), BasicEmitStore.CIn(mult, mult.Type.Terminals["Num2"]));
                        }

                        context.Connect(BasicEmitStore.COut(mult, mult.Type.Terminals["Num1 * Num2"]), BasicEmitStore.CIn(add, add.Type.Terminals["Num2"]));
                    }

                    return BasicEmitStore.COut(add, add.Type.Terminals["Num1 + Num2"]);
                });

            [FunctionDoc(
                NameOverwrite = "Lerp",
                Info = """
                Linearly interpolates between <link type="param">from</> and <link type="param">to</>, depending on <link type="param">amount</>.
                """,
                ReturnValueInfo = """
                The value between <link type="param">from</> and <link type="param">to</>.
                """,
                ParameterInfos = [
                    """
                    The start value.
                    """,
                    """
                    The end value.
                    """,
                    """
                    How far between <link type="param">from</> and <link type="param">to</> to transition (0 - 1).
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                // ease out from "from" to "to"
                vec3 from
                vec3 to
                float speed
                on Play()
                {
                    from = vec3(0, 0, 0)
                    to = vec3(0, 90, 0)
                    speed = 0.05 // 5% between from and to
                }
                from = lerp(from, to, speed)
                </>
                """
            )]
            public static readonly FunctionSymbol LerpVec
                = new BuiltinFunctionSymbol(mathNamespace, "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Vector3),
                    new ParameterSymbol("to", TypeSymbol.Vector3),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ], TypeSymbol.Vector3, (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

                    if (constants is not null)
                    {
                        Vector3F from = (constants[0] as Vector3F?) ?? Vector3F.Zero;
                        Vector3F to = (constants[1] as Vector3F?) ?? Vector3F.Zero;
                        float amount = (constants[2] as float?) ?? 0f;

                        Block vec = context.AddBlock(Blocks.Values.Vector);
                        context.SetBlockValue(vec, 0, from + (to - from) * amount);

                        return BasicEmitStore.COut(vec, vec.Type.Terminals["Vector"]);
                    }

                    Block add = context.AddBlock(Blocks.Math.Add_Vector);

                    using (context.ExpressionBlock())
                    {
                        context.Connect(context.EmitExpression(call.Arguments[0]), BasicEmitStore.CIn(add, add.Type.Terminals["Vec1"]));

                        Block mult = context.AddBlock(Blocks.Math.Multiply_Vector);

                        using (context.ExpressionBlock())
                        {
                            Block sub = context.AddBlock(Blocks.Math.Subtract_Vector);

                            using (context.ExpressionBlock())
                            {
                                context.Connect(context.EmitExpression(call.Arguments[1]), BasicEmitStore.CIn(sub, sub.Type.Terminals["Vec1"]));
                                context.Connect(context.EmitExpression(call.Arguments[0]), BasicEmitStore.CIn(sub, sub.Type.Terminals["Vec2"]));
                            }

                            context.Connect(BasicEmitStore.COut(sub, sub.Type.Terminals["Vec1 - Vec2"]), BasicEmitStore.CIn(mult, mult.Type.Terminals["Vec"]));

                            context.Connect(context.EmitExpression(call.Arguments[2]), BasicEmitStore.CIn(mult, mult.Type.Terminals["Num"]));
                        }

                        context.Connect(BasicEmitStore.COut(mult, mult.Type.Terminals["Vec * Num"]), BasicEmitStore.CIn(add, add.Type.Terminals["Vec2"]));
                    }

                    return BasicEmitStore.COut(add, add.Type.Terminals["Vec1 + Vec2"]);
                });

            [FunctionDoc(
                Info = """
                Outputs rotation of <link type="param">angle</> around <link type="param">axis</>.
                """,
                ParameterInfos = [
                    """
                    The axis to rotate around.
                    """,
                    """
                    How much to rotate (in degrees).
                    """
                ],
                Examples = """
                <codeblock lang="fcs">
                inspect(axisAngle(vec3(0, 1, 0), 90)) // (0, 90, 0))

                inspect(axisAngle(vec3(1, 0, 0), 45)) // (45, 0, 0)
                </>
                """
            )]
            public static readonly FunctionSymbol AxisAngle
                = new BuiltinFunctionSymbol(mathNamespace, "axisAngle",
                [
                    new ParameterSymbol("axis", TypeSymbol.Vector3),
                    new ParameterSymbol("angle", TypeSymbol.Float),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.AxisAngle));

            [FunctionDoc(
                Info = """
                Gets a ray going from ($plink screenX;, $plink screenY;).
                """,
                ParameterInfos = [
                    """
                    The x screen coordinate.
                    """,
                    """
                    The y screen coordinate.
                    """,
                    """
                    Position 2 units away from the camera.
                    """,
                    """
                    Position 400 units away from the camera.
                    """
                ],
                Remarks = [
                    """
                    Due to a technical issue, still to be fixed, the output lags by one frame. I.e. if you Set Camera on frame N, then this block's output will change on frame N+1 - the function will not work on the first frame.
                    """
                ],
                Related = [
                    """
                    <link type="func">worldToScreen;vec3;float;float</>
                    """,
                    """
                    <link type="func">worldToScreen;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol ScreenToWorld
                = new BuiltinFunctionSymbol(mathNamespace, "screenToWorld",
                [
                    new ParameterSymbol("screenX", TypeSymbol.Float),
                    new ParameterSymbol("screenY", TypeSymbol.Float),
                    new ParameterSymbol("worldNear", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("worldFar", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.ScreenToWorld));

            [FunctionDoc(
                Info = """
                Gets at what screen position is <link type="param">worldPos</> located.
                """,
                Remarks = [
                    """
                    Due to a technical issue, still to be fixed, the output lags by one frame. I.e. if you Set Camera on frame N, then this block's output will change on frame N+1 - the function will not work on the first frame.
                    """
                ],
                Related = [
                    """
                    <link type="func">worldToScreen;vec3</>
                    """,
                    """
                    <link type="func">screenToWorld;float;float;vec3;vec3</>
                    """,
                ]
            )]
            public static readonly FunctionSymbol WorldToScreen
                = new BuiltinFunctionSymbol(mathNamespace, "worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3),
                    new ParameterSymbol("screenX", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("screenY", Modifiers.Out, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.WorldToScreen));

            [FunctionDoc(
                Info = """
                Returns at what screen position is <link type="param">worldPos</> located.
                """,
                ReturnValueInfo = """
                The screen position of <link type="param">worldPos</> (X, Y).
                """,
                Remarks = [
                    """
                    Due to a technical issue, still to be fixed, the output lags by one frame. I.e. if you Set Camera on frame N, then this block's output will change on frame N+1 - the function will not work on the first frame.
                    """
                ],
                Related = [
                    """
                    <link type="func">worldToScreen;vec3;float;float</>
                    """,
                    """
                    <link type="func">screenToWorld;float;float;vec3;vec3</>
                    """,
                ]
            )]
            public static readonly FunctionSymbol WorldToScreen2
                = new BuiltinFunctionSymbol(mathNamespace, "worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) =>
                {
                    Block make = context.AddBlock(Blocks.Math.Make_Vector);

                    using (context.ExpressionBlock())
                    {
                        Block wts = context.AddBlock(Blocks.Math.WorldToScreen);

                        using (context.ExpressionBlock())
                        {
                            EmitStore store = context.EmitExpression(call.Arguments[0]);
                            context.Connect(store, BasicEmitStore.CIn(wts, wts.Type.Terminals["World Pos"]));
                        }

                        context.Connect(BasicEmitStore.COut(wts, wts.Type.Terminals["Screen X"]), BasicEmitStore.CIn(make, make.Type.Terminals["X"]));
                        context.Connect(BasicEmitStore.COut(wts, wts.Type.Terminals["Screen Y"]), BasicEmitStore.CIn(make, make.Type.Terminals["Y"]));
                    }

                    return BasicEmitStore.COut(make, make.Type.Terminals["Vector"]);
                });

            [FunctionDoc(
                Info = """
                Returns a rotation pointing in <link type="param">direction</>.
                """,
                ReturnValueInfo = """
                Rotation "looking" in <link type="param">direction</>.
                """,
                ParameterInfos = [
                    """
                    The direction to point in.
                    """,
                    """
                    The up direction (default is (0, 1, 0)).
                    """
                ]
            )]
            public static readonly FunctionSymbol LookRotation
                = new BuiltinFunctionSymbol(mathNamespace, "lookRotation",
                [
                    new ParameterSymbol("direction", TypeSymbol.Vector3),
                    new ParameterSymbol("up", TypeSymbol.Vector3),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.LookRotation));

            [FunctionDoc(
                Info = """
                Returns the point at which a line intersects a plane.
                """,
                ReturnValueInfo = """
                The intersection of a line and a plane.
                """,
                ParameterInfos = [
                    """
                    Line's starting position.
                    """,
                    """
                    Line's end position.
                    """,
                    """
                    A point on the plane.
                    """,
                    """
                    A vector perpendicular to the plane (the up direction from the plane's surface).
                    """
                ],
                Remarks = [
                    """
                    The line is not a line segment, so the intersection will be found even if it's not in-between <link type="param">lineFrom</>/<link type="param">lineTo</>.
                    """
                ]
            )]
            public static readonly FunctionSymbol LineVsPlane
                = new BuiltinFunctionSymbol(mathNamespace, "lineVsPlane",
                [
                    new ParameterSymbol("lineFrom", TypeSymbol.Vector3),
                    new ParameterSymbol("lineTo", TypeSymbol.Vector3),
                    new ParameterSymbol("planePoint", TypeSymbol.Vector3),
                    new ParameterSymbol("planeNormal", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.LineVsPlane));
        }

        [FunctionDoc(
            Info = """
            Displays <link type="param">value</>.
            """,
            Examples = """
            <codeblock lang="fcs">
            vec3 pos
            // ...
            inspect(pos)
            </>
            """
        )]
        public static readonly FunctionSymbol Inspect
            = new BuiltinFunctionSymbol(builtinNamespace, "inspect",
            [
                new ParameterSymbol("value", TypeSymbol.Generic)
            ], TypeSymbol.Void, [TypeSymbol.Bool, TypeSymbol.Float, TypeSymbol.Vector3, TypeSymbol.Rotation, TypeSymbol.Object], (call, context) =>
                {
                    Block inspect = context.AddBlock(Blocks.Values.InspectByType(call.GenericType!.ToWireType()));

                    using (context.ExpressionBlock())
                    {
                        EmitStore store = context.EmitExpression(call.Arguments[0]);

                        context.Connect(store, BasicEmitStore.CIn(inspect, inspect.Type.TerminalArray[1]));
                    }

                    return new BasicEmitStore(inspect);
                }
            );

        [FunctionDoc(
            Info = """
            Returns the value (reference) from <link type="param">array</> at <link type="param">index</>.
            """,
            ReturnValueInfo = """
            The value at <link type="param">index</>.
            """,
            Examples = """
            <codeblock lang="fcs">
            array<float> arr = [3, 2, 1]
            inspect(arr.get(1)) // 2
            </>
            """
        )]
        public static readonly FunctionSymbol Array_Get
            = new BuiltinFunctionSymbol(builtinNamespace, "get",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
            ], TypeSymbol.Generic, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
                    var indexArg = call.Arguments[1];

                    // optimize for when index is 0
                    if (indexArg.ConstantValue is not null && (float)indexArg.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
                    {
                        return context.EmitExpression(call.Arguments[0]);
                    }

                    Block list = context.AddBlock(Blocks.Variables.ListByType(call.GenericType!.ToWireType()));

                    using (context.ExpressionBlock())
                    {
                        EmitStore array = context.EmitExpression(call.Arguments[0]);
                        EmitStore index = context.EmitExpression(call.Arguments[1]);

                        context.Connect(array, BasicEmitStore.CIn(list, list.Type.Terminals["Variable"]));
                        context.Connect(index, BasicEmitStore.CIn(list, list.Type.Terminals["Index"]));
                    }

                    return BasicEmitStore.COut(list, list.Type.Terminals["Element"]);
                }
            )
            {
                IsMethod = true,
            };

        [FunctionDoc(
            Info = """
            Sets <link type="param">array</> at <link type="param">index</> to <link type="param">value</>.
            """,
            Examples = """
            <codeblock lang="fcs">
            array<float> arr
            on Loop(0, 5, out inline float i)
            {
                arr.set(i, i + 1)
            }
            // arr - [1, 2, 3, 4, 5]
            </>
            """
        )]
        public static readonly FunctionSymbol Array_Set
            = new BuiltinFunctionSymbol(builtinNamespace, "set",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
                new ParameterSymbol("value", TypeSymbol.Generic),
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
                    var indexArg = call.Arguments[1];

                    // optimize for when index is 0
                    if (indexArg.ConstantValue is not null && (float)indexArg.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
                    {
                        return context.EmitSetExpression(call.Arguments[0], () =>
                        {
                            using (context.ExpressionBlock())
                                return context.EmitExpression(call.Arguments[2]);
                        });
                    }

                    Block setPtr = context.AddBlock(Blocks.Variables.Set_PtrByType(call.GenericType!.ToWireType()));

                    using (context.ExpressionBlock())
                    {
                        Block list = context.AddBlock(Blocks.Variables.ListByType(call.GenericType!.ToWireType()));

                        using (context.ExpressionBlock())
                        {
                            EmitStore array = context.EmitExpression(call.Arguments[0]);
                            EmitStore index = context.EmitExpression(call.Arguments[1]);

                            context.Connect(array, BasicEmitStore.CIn(list, list.Type.Terminals["Variable"]));
                            context.Connect(index, BasicEmitStore.CIn(list, list.Type.Terminals["Index"]));
                        }

                        EmitStore value = context.EmitExpression(call.Arguments[2]);

                        context.Connect(BasicEmitStore.COut(list, list.Type.Terminals["Element"]), BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals["Variable"]));
                        context.Connect(value, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals["Value"]));
                    }

                    return new BasicEmitStore(setPtr);
                }
            )
            {
                IsMethod = true,
            };

        [FunctionDoc(
            Info = """
            Sets a range of <link type="param">array</>, starting at <link type="param">index</>.
            """,
            Examples = """
            <codeblock lang="fcs">
            array<float> arr
            arr.setRange(2, [1, 2, 3])
            // arr - [0, 0, 1, 2, 3]
            </>
            """
        )]
        public static readonly FunctionSymbol Array_SetRange
            = new BuiltinFunctionSymbol(builtinNamespace, "setRange",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
                new ParameterSymbol("value", TypeSymbol.ArraySegment),
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
            {
                var array = call.Arguments[0];
                var index = call.Arguments[1];
                var segment = (BoundArraySegmentExpression)call.Arguments[2];

                Debug.Assert(segment.Elements.Length > 0);

                return context.EmitSetArraySegment(segment, array, index);
            })
            {
                IsMethod = true,
            };

        [FunctionDoc(
            Info = """
            Sets the value of a variable/array element.
            """,
            ParameterInfos = [
                """
                The variable/array element to set.
                """,
                """
                The value to set <link type="param">pointer</> to.
                """
            ],
            Examples = """
            <codeblock lang="fcs">
            array<float> arr
            // ...
            inline float first = arr.get(0)
            inspect(first)
            setPtrValue(first, 10)
            // arr - [10, ...]
            </>
            """
        )]
        public static readonly FunctionSymbol Ptr_SetValue
            = new BuiltinFunctionSymbol(builtinNamespace, "setPtrValue",
            [
                new ParameterSymbol("pointer", TypeSymbol.Generic),
                new ParameterSymbol("value", TypeSymbol.Generic),
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
                    Block setPtr = context.AddBlock(Blocks.Variables.Set_PtrByType(call.GenericType!.ToWireType()));

                    using (context.ExpressionBlock())
                    {
                        EmitStore ptr = context.EmitExpression(call.Arguments[0]);
                        EmitStore value = context.EmitExpression(call.Arguments[1]);

                        context.Connect(ptr, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals["Variable"]));
                        context.Connect(value, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals["Value"]));
                    }

                    return new BasicEmitStore(setPtr);

                }
            );

        [FunctionDoc(
            Info = """
            Creates a comment, that gets emitted into fancade as comment blocks.
            """,
            ParameterInfos = [
                """
                The text of the comment.
                """
            ],
            Examples = """
            <codeblock lang="fcs">
            // comment only visible in code
            fcComment("comment also visible in fancade")
            </>
            """
        )]
        public static readonly FunctionSymbol FcComment
            = new BuiltinFunctionSymbol(builtinNamespace, "fcComment",
            [
                new ParameterSymbol("TEXT", Modifiers.Constant, TypeSymbol.String)
            ], TypeSymbol.Void, (call, context) =>
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                if (constants is not null && constants[0] is string text && !string.IsNullOrEmpty(text))
                    context.WriteComment(text);

                return NopEmitStore.Instance;
            }
            );

        [FunctionDoc(
            Info = """
            Returns a block with the specified id.
            """,
            ReturnValueInfo = """
            The <link type="type">obj</> specified by <link type="param">BLOCK</>.
            """,
            ParameterInfos = [
                """
                Id of the block, one of <link type="con">BLOCK</>.
                """
            ],
            Remarks = [
                """
                The id of a block can be get by placing the block at (0, 0, 0) and running log(getBlock(0, 0, 0)) in the EditorScript.
                """,
                """
                EditorScript cannot connect wires to blocks, so if you are using it as the builder, you will have to connect the object wire to the block manually.
                """
            ],
            Examples = """
            <codeblock lang="fcs">
            obj dirt = getBlockById(BLOCK_DIRT)
            </>
            """
        )]
        public static readonly FunctionSymbol GetBlockById
            = new BuiltinFunctionSymbol(builtinNamespace, "getBlockById",
            [
                new ParameterSymbol("BLOCK", Modifiers.Constant, TypeSymbol.Float)
            ], TypeSymbol.Object, (call, context) =>
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                if (constants is null)
                    return NopEmitStore.Instance;

                float _id = (constants[0] as float?) ?? 0f;
                ushort id = (ushort)System.Math.Clamp((int)_id, ushort.MinValue, ushort.MaxValue);

                BlockDef def;
                if (id < RawGame.CurrentNumbStockPrefabs)
                {
                    var groupEnumerable = StockPrefabs.Instance.List.GetGroup(id);

                    if (groupEnumerable.Any())
                    {
                        PartialPrefabGroup group = new PartialPrefabGroup(groupEnumerable);
                        PartialPrefab mainPrefab = group[Vector3B.Zero];

                        def = new BlockDef(mainPrefab.Name, id, BlockType.NonScript, group.Size, []);
                    }
                    else
                        def = new BlockDef(string.Empty, id, BlockType.NonScript, new Vector2I(1, 1));
                }
                else
                    def = new BlockDef(string.Empty, id, BlockType.NonScript, new Vector2I(1, 1));

                Block block = context.AddBlock(def);

                if (context.Builder is not IConnectToBlocksBuilder)
                {
                    context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);
                    return NopEmitStore.Instance;
                }

                return new BasicEmitStore(new NopConnectTarget(), [new BlockVoxelConnectTarget(block)]);
            }
            );

        [FunctionDoc(
            Info = """
            *Dirrectly* convers <link type="param">vec</> to <link type="type">rot</>.
            """
        )]
        public static readonly FunctionSymbol ToRot
           = new BuiltinFunctionSymbol(builtinNamespace, "toRot",
           [
               new ParameterSymbol("vec", TypeSymbol.Vector3)
           ], TypeSymbol.Rotation, (call, context) =>
           {
               object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

               if (constants is not null)
               {
                   Vector3F vec = (constants[0] as Vector3F?) ?? Vector3F.Zero;

                   Block block = context.AddBlock(Blocks.Values.Rotation);

                   context.SetBlockValue(block, 0, new Rotation(vec));

                   return BasicEmitStore.COut(block, block.Type.Terminals["Rotation"]);
               }
               else
               {
                   Block make = context.AddBlock(Blocks.Math.Make_Rotation);

                   using (context.ExpressionBlock())
                   {
                       var (x, y, z) = context.BreakVector(call.Arguments[0]);

                       context.Connect(x, BasicEmitStore.CIn(make, make.Type.Terminals["X angle"]));
                       context.Connect(y, BasicEmitStore.CIn(make, make.Type.Terminals["Y angle"]));
                       context.Connect(z, BasicEmitStore.CIn(make, make.Type.Terminals["Z angle"]));
                   }

                   return BasicEmitStore.COut(make, make.Type.Terminals["Rotation"]);
               }
           }
           )
           {
               IsMethod = true
           };

        [FunctionDoc(
            Info = """
            *Dirrectly* convers <link type="param">rot</> to <link type="type">vec3</>.
            """
        )]
        public static readonly FunctionSymbol ToVec
           = new BuiltinFunctionSymbol(builtinNamespace, "toVec",
           [
               new ParameterSymbol("rot", TypeSymbol.Rotation)
           ], TypeSymbol.Vector3, (call, context) =>
           {
               object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

               if (constants is not null)
               {
                   Rotation rot = (constants[0] as Rotation) ?? new Rotation(Vector3F.Zero);

                   Block block = context.AddBlock(Blocks.Values.Vector);

                   context.SetBlockValue(block, 0, rot);

                   return BasicEmitStore.COut(block, block.Type.Terminals["Vector"]);
               }
               else
               {
                   Block make = context.AddBlock(Blocks.Math.Make_Vector);

                   using (context.ExpressionBlock())
                   {
                       var (x, y, z) = context.BreakVector(call.Arguments[0]);

                       context.Connect(x, BasicEmitStore.CIn(make, make.Type.Terminals["X"]));
                       context.Connect(y, BasicEmitStore.CIn(make, make.Type.Terminals["Y"]));
                       context.Connect(z, BasicEmitStore.CIn(make, make.Type.Terminals["Z"]));
                   }

                   return BasicEmitStore.COut(make, make.Type.Terminals["Vector"]);
               }
           }
           )
           {
               IsMethod = true
           };

        //public static readonly FunctionSymbol PlayMidi
        //    = new BuiltinFunctionSymbol("playMidi",
        //        [
        //            new ParameterSymbol("fileName", TypeSymbol.String),
        //        ], TypeSymbol.Void, (call, context) =>
        //        {
        //            object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

        //            if (constants is null)
        //                return NopEmitStore.Instance;

        //            string? path = constants[0] as string;

        //            if (string.IsNullOrWhiteSpace(path))
        //                throw new Exception();

        //            MidiFile file;
        //            using (FileStream stream = File.OpenRead(path))
        //                file = MidiFile.Read(stream, new ReadingSettings());

        //            MidiConvertSettings convertSettings = MidiConvertSettings.Default;
        //            convertSettings.MaxFrames = 60 * 45;

        //            MidiConverter converter = new MidiConverter(file, convertSettings);

        //            FcSong song = converter.Convert();
        //            var (blocksSize, blocks) = song.ToBlocks();

        //            EmitConnector connector = new EmitConnector(context.Connect);

        //            // channel event structure:
        //            // t - type
        //            // d - delta time since last event (in frames)
        //            // a - data0 (optional - depends on type)
        //            // b - data1 (optional - depends on type)
        //            // tttd_dddd - z_pos: 0
        //            // aaaa_aaaa - z_pos: 1
        //            // bbbb_bbbb - z_pos: 2

        //            // TODO: emit set variable originObj to originBlock variable

        //            SyntaxTree tree = SyntaxTree.Parse(SourceText.From($$"""
        //            global array<vec3> midi_c_pos // channel pos
        //            global array<float> midi_c_si // channel sound index
        //            global array<float> midi_c_wt // channel wait time

        //            on Play
        //            {
        //                obj originObj
        //                originObj.getPos(out global vec3 origin, out _)
        //                origin.y -= .5
        //                on Loop(0, {{song.Channels.Count}}, out inline float channelIndex)
        //                {
        //                    midi_c_pos.set(channelIndex, origin + vec3(0, 0, channelIndex * {{FcSong.ChannelSize.X}}))
        //                    midi_c_wt.set(channelIndex, -1)
        //                }
        //            }

        //            on Loop(0, {{song.Channels.Count}}, out inline float channelIndex)
        //            {
        //                //inline float channelIndex = _channelIndex * {{FcSong.ChannelSize.X}}
        //                inline vec3 midi_pos_ref = midi_c_pos.get(channelIndex)
        //                inline float midi_t_ref = midi_c_wt.get(channelIndex)

        //                while (true)
        //                {
        //                    vec3 midi_pos = midi_c_pos.get(channelIndex)
        //                    float midi_t = midi_c_wt.get(channelIndex)

        //                    inspect(midi_pos)
        //                    inspect(midi_t)

        //                    if (midi_t > 0)
        //                    {
        //                        midi_t_ref--
        //                        break
        //                    }
        //                    else
        //                    {
        //                        // read event type
        //                        readBinary(midi_pos, 3, out float midi_et)
        //                        // read event delta (time since last event)
        //                        readBinary(midi_pos + vec3(0, 3, 0), 5, out float midi_ed)

        //                        inspect(midi_ed)

        //                        if (midi_t == -1 && midi_ed > 0)
        //                        {
        //                            setPtrValue(midi_t_ref, midi_ed)
        //                            break
        //                        }
        //                        else
        //                        {
        //                            inspect(midi_et)

        //                            if (midi_et < 0.5)
        //                            {
        //                                // wait
        //                                readBinary(midi_pos + vec3(1, 0, 0), 8, out float midi_w_d)

        //                                setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0)) 
        //                                setPtrValue(midi_t_ref, midi_w_d)
        //                                break
        //                            }
        //                            else if (midi_et < 1.5)
        //                            {
        //                                // play note
        //                                readBinary(midi_pos + vec3(1, 0, 0), 7, out float midi_n_n)
        //                                readBinary(midi_pos + vec3(1, 7, 0), 1, out float midi_n_hv) // has non default velocity (not 255)
        //                                float midi_n_v
        //                                if (midi_n_hv > 0.5)
        //                                {
        //                                    readBinary(midi_pos + vec3(2, 0, 0), 8, out midi_n_v)
        //                                    setPtrValue(midi_pos_ref, midi_pos + vec3(3, 0, 0)) 
        //                                }
        //                                else
        //                                {
        //                                    midi_n_v = 255
        //                                    setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0)) 
        //                                }

        //                                // stop current note if playing - shouldn't be needed, but...
        //                                stopSound(midi_c_si.get(channelIndex))

        //                                // https://discord.com/channels/409219533618806786/464440459410800644/1224463893058031778
        //                                playSound(midi_n_v / 255, pow(2, /*(*/midi_n_n /*- 60)*/ / 12), out float channel, false, SOUND_PIANO)
        //                                midi_c_si.set(channelIndex, channel)

        //                                setPtrValue(midi_t_ref, -1)
        //                            }
        //                            else if (midi_et < 2.5)
        //                            {
        //                                // stop current note
        //                                stopSound(midi_c_si.get(channelIndex))

        //                                setPtrValue(midi_pos_ref, midi_pos + vec3(1, 0, 0))
        //                                setPtrValue(midi_t_ref, -1)
        //                            }
        //                            else if (midi_et < 3.5)
        //                            {
        //                                // set instrument, nop for now
        //                                setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0))
        //                                setPtrValue(midi_t_ref, -1)
        //                            }
        //                            else
        //                            {
        //                                // prevent infinite loop
        //                                setPtrValue(midi_pos_ref, midi_pos + vec3(1, 0, 0))
        //                                setPtrValue(midi_t_ref, -1)
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //            func readBinary(vec3 pos, float len, out float value)
        //            {
        //                float y = pos.y + 0.5//0.9375

        //                value = 0
        //                float bitVal = 1

        //                on Loop(0, len, out inline float i)
        //                {
        //                    raycast(vec3(pos.x, y + 0.625/*0.125*/, pos.z), vec3(pos.x, y, pos.z), out bool didHit, out _, out _)

        //                    if (didHit)
        //                        value += bitVal

        //                    y++
        //                    bitVal *= 2
        //                }

        //                value = round(value)
        //            }
        //            """));

        //            Compilation compilation = Compilation.CreateScript(null, tree);

        //            // TODO: Replace with Debug.Assert lenght == 0
        //            var diagnostics = compilation.Emit(new TowerCodePlacer(context.Builder), context.Builder);
        //            if (diagnostics.Length != 0)
        //                throw new Exception(diagnostics[0].ToString());

        //            Block originBlock = new Block(Vector3I.Zero, new BlockDef(string.Empty, 512, BlockType.NonScript, new Vector2I(1, 1)));

        //            context.Builder.AddBlockSegments(
        //                new Block[] { originBlock }
        //                    .Concat(blocks.Select(pos => new Block(pos, Blocks.Shrub)))
        //            );

        //            return connector.Store;
        //        });

        public static void Init()
        {
            RuntimeHelpers.RunClassConstructor(typeof(BuiltinFunctions).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(Control).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(Math).TypeHandle);
        }

        private static IEnumerable<FunctionSymbol>? functionsCache;
        internal static IEnumerable<FunctionSymbol> GetAll()
            => functionsCache ??= typeof(BuiltinFunctions)
            .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static)
            .SelectMany(([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type) => type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol)f.GetValue(null)!)
            )
            .Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol)f.GetValue(null)!)
            );
    }
}
