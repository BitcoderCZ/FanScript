﻿using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.Utils;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]
namespace FanScript.Compiler.Symbols
{
    internal static class BuiltinFunctions
    {
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
                            val = RuntimeHelpers.GetUninitializedObject(type);
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
            [FunctionDoc(
                ParameterInfos = [
                    """
                    Time to win (in frames), must be constant.
                    """
                ]
            )]
            public static readonly FunctionSymbol Win
                = new BuiltinFunctionSymbol("win", [
                    new ParameterSymbol("DELAY", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Win, constantTypes: [typeof(byte)]));

            [FunctionDoc(
                ParameterInfos = [
                    """
                    Time to lose (in frames), must be constant.
                    """
                ]
            )]
            public static readonly FunctionSymbol Lose
                = new BuiltinFunctionSymbol("lose", [
                    new ParameterSymbol("DELAY", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Lose, constantTypes: [typeof(byte)]));

            [FunctionDoc(
                Info = """
                Sets the <link type="param">score</> and/or <link type="param">coins</>.
                """,
                ParameterInfos = [
                    """
                    The new score, if <link type="param">RANKING</link> is <link type="con_value">RANKING;RANKING_TIME_FASTEST</> or <link type="con_value">RANKING;RANKING_TIME_LONGEST</> time is specified in frames (60 - 1s).
                    """,
                    """
                    The new amount of coins.
                    """,
                    """
                    How players are ranked, one of <link type="con">RANKING</>, must be constant.
                    """
                ]
            )]
            public static readonly FunctionSymbol SetScore
                = new BuiltinFunctionSymbol("setScore", [
                    new ParameterSymbol("score", TypeSymbol.Float),
                    new ParameterSymbol("coins", TypeSymbol.Float),
                    new ParameterSymbol("RANKING", TypeSymbol.Float),
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
                    <list>If in orthographic (isometric) mode, determines how wide the view frustum is.</>
                    <list>If in perspective mode specifies half of the field of view.</>
                    """,
                    """
                    If true, the camera will be in perspective mode, otherwise it will be in orthographic mode.
                    """
                ]
            )]
            public static readonly FunctionSymbol SetCamera
                = new BuiltinFunctionSymbol("setCamera", [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                    new ParameterSymbol("range", TypeSymbol.Float),
                    new ParameterSymbol("PERSPECTIVE", TypeSymbol.Bool),
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
                = new BuiltinFunctionSymbol("setLight", [
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
                = new BuiltinFunctionSymbol("getScreenSize", [
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
               = new BuiltinFunctionSymbol("getScreenSize", [], TypeSymbol.Vector3, (call, context) =>
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
                = new BuiltinFunctionSymbol("getAccelerometer", [], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Game.Accelerometer));

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
                = new BuiltinFunctionSymbol("getCurrentFrame", [], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Game.CurrentFrame));

            [FunctionDoc(
                Info = """
                Creates a section in the shop, calls to <link type="func">menuItem,float,obj,string,float,float</> after this will create items in this section.
                """,
                ParameterInfos = [
                    """
                    The name of this section, will be shown as a header above the items, must be constant.
                    """
                ],
                Related = [
                    """
                    <link type="func">menuItem,float,obj,string,float,float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol ShopSection
                = new BuiltinFunctionSymbol("shopSection", [
                    new ParameterSymbol("NAME", TypeSymbol.String),
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
                    Name of the item, must be constant.
                    """,
                    """
                    Maximum number of times the item can be bought, can be 2-100 or one of <link type="con">MAX_ITEMS</>, must be constant.
                    """,
                    """
                    Specifies what the initial price is and how it increases, one of <link type="con">PRICE_INCREASE</>, must be constant.
                    """
                ],
                Related = [
                    """
                    <link type="func">shopSection;string</>
                    """
                ]
            )]
            public static readonly FunctionSymbol MenuItem
                = new BuiltinFunctionSymbol("menuItem", [
                    new ParameterSymbol("variable", Modifiers.Ref, TypeSymbol.Float),
                    new ParameterSymbol("picture", TypeSymbol.Object),
                    new ParameterSymbol("NAME", TypeSymbol.String),
                    new ParameterSymbol("MAX_ITEMS", TypeSymbol.Float),
                    new ParameterSymbol("PRICE_INCREASE", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.MenuItem, constantTypes: [typeof(string), typeof(byte), typeof(byte)]));
        }

        private static class Objects
        {
            [FunctionDoc(
                Info = """
                Returns the object at <link type="param">position</>.
                """,
                ReturnValueInfo = """
                The object at <link type="param">position</>.
                """,
                ParameterInfos = [
                    """
                    Position of the object, must be constant.
                    """
                ],
                Related = [
                    """
                    <link type="func">getObject;float;float;float</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetObject
              = new BuiltinFunctionSymbol("getObject", [
                  new ParameterSymbol("position", TypeSymbol.Vector3),
              ], TypeSymbol.Object, (call, context) =>
              {
                  BoundConstant? constant = call.Arguments[0].ConstantValue;
                  if (constant is null)
                  {
                      context.Diagnostics.ReportValueMustBeConstant(call.Arguments[0].Syntax.Location);
                      return NopEmitStore.Instance;
                  }

                  Vector3I pos = (Vector3I)((Vector3F)constant.GetValueOrDefault(TypeSymbol.Vector3)); // unbox, then cast

                  if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);

                      using (context.ExpressionBlock())
                          context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");

                      return NopEmitStore.Instance;
                  }

                  return new AbsoluteEmitStore(pos, null);
              }
              );

            [FunctionDoc(
                Info = """
                Returns the object at (<link type="param">x</>, <link type="param">y</>, <link type="param">z</>).
                """,
                ReturnValueInfo = """
                The object at (<link type="param">x</>, <link type="param">y</>, <link type="param">z</>).
                """,
                ParameterInfos = [
                    """
                    X position of the object, must be constant.
                    """,
                    """
                    Y position of the object, must be constant.
                    """,
                    """
                    Z position of the object, must be constant.
                    """
                ],
                Related = [
                    """
                    <link type="func">getObject;vec3</>
                    """
                ]
            )]
            public static readonly FunctionSymbol GetObject2
              = new BuiltinFunctionSymbol("getObject", [
                  new ParameterSymbol("x", TypeSymbol.Float),
                  new ParameterSymbol("y", TypeSymbol.Float),
                  new ParameterSymbol("z", TypeSymbol.Float),
              ], TypeSymbol.Object, (call, context) =>
              {
                  object?[]? args = context.ValidateConstants(call.Arguments.AsMemory(), true);
                  if (args is null)
                      return NopEmitStore.Instance;

                  Vector3I pos = new Vector3I((int)((float?)args[0] ?? 0f), (int)((float?)args[1] ?? 0f), (int)((float?)args[2] ?? 0f)); // unbox, then cast

                  if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);

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
                = new BuiltinFunctionSymbol("setPos", [
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
                = new BuiltinFunctionSymbol("setPos",
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
                = new BuiltinFunctionSymbol("getPos", [
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
                = new BuiltinFunctionSymbol("raycast", [
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
                = new BuiltinFunctionSymbol("getSize", [
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
                = new BuiltinFunctionSymbol("setVisible", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("visible", TypeSymbol.Bool),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.SetVisible))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol Clone
                = new BuiltinFunctionSymbol("clone", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("copy", Modifiers.Out, TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitAXX(call, context, 1, Blocks.Objects.CreateObject))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol Destroy
                = new BuiltinFunctionSymbol("destroy", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.DestroyObject))
                {
                    IsMethod = true,
                };
        }

        private static class Sound
        {
            public static readonly FunctionSymbol PlaySound
                  = new BuiltinFunctionSymbol("playSound",
                  [
                    new ParameterSymbol("volume", TypeSymbol.Float),
                      new ParameterSymbol("pitch", TypeSymbol.Float),
                      new ParameterSymbol("channel", Modifiers.Out, TypeSymbol.Float),
                      new ParameterSymbol("LOOP", TypeSymbol.Bool),
                      new ParameterSymbol("SOUND", TypeSymbol.Float),
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
            public static readonly FunctionSymbol StopSound
                 = new BuiltinFunctionSymbol("stopSound",
                 [
                    new ParameterSymbol("channel", TypeSymbol.Float),
                 ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Sound.StopSound));
            public static readonly FunctionSymbol SetVolumePitch
                 = new BuiltinFunctionSymbol("setVolumePitch",
                 [
                    new ParameterSymbol("channel", TypeSymbol.Float),
                     new ParameterSymbol("volume", TypeSymbol.Float),
                     new ParameterSymbol("pitch", TypeSymbol.Float),
                 ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Sound.VolumePitch));
        }

        private static class Physics
        {
            public static readonly FunctionSymbol AddForce
                = new BuiltinFunctionSymbol("addForce",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("force", TypeSymbol.Vector3),
                    new ParameterSymbol("applyAt", TypeSymbol.Vector3),
                    new ParameterSymbol("torque", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AddForce))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol GetVelocity
                = new BuiltinFunctionSymbol("getVelocity",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("velocity", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("spin", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Physics.GetVelocity))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetVelocity
                = new BuiltinFunctionSymbol("setVelocity",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("velocity", TypeSymbol.Vector3),
                    new ParameterSymbol("spin", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetVelocity))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetLocked
                = new BuiltinFunctionSymbol("setLocked",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetLocked))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetMass
                = new BuiltinFunctionSymbol("setMass",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("mass", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetMass))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetFriction
                = new BuiltinFunctionSymbol("setFriction",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("friction", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetFriction))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetBounciness
                = new BuiltinFunctionSymbol("setBounciness",
                [
                   new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("bounciness", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetBounciness))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol SetGravity
                = new BuiltinFunctionSymbol("setGravity",
                [
                   new ParameterSymbol("gravity", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.SetGravity));
            public static readonly FunctionSymbol AddConstraint
                = new BuiltinFunctionSymbol("addConstraint",
                [
                   new ParameterSymbol("base", TypeSymbol.Object),
                    new ParameterSymbol("part", TypeSymbol.Object),
                    new ParameterSymbol("pivot", TypeSymbol.Vector3),
                    new ParameterSymbol("constraint", Modifiers.Out, TypeSymbol.Constraint),
                ], TypeSymbol.Void, (call, context) => emitAXX(call, context, 1, Blocks.Physics.AddConstraint))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol LinearLimits
                = new BuiltinFunctionSymbol("linearLimits",
                [
                   new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("lower", TypeSymbol.Vector3),
                    new ParameterSymbol("upper", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearLimits))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol AngularLimits
                = new BuiltinFunctionSymbol("angularLimits",
                [
                   new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("lower", TypeSymbol.Vector3),
                    new ParameterSymbol("upper", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AngularLimits))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol LinearSpring
                = new BuiltinFunctionSymbol("linearSpring",
                [
                   new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("stiffness", TypeSymbol.Vector3),
                    new ParameterSymbol("damping", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearSpring))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol AngularSpring
                = new BuiltinFunctionSymbol("angularSpring",
                [
                   new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("stiffness", TypeSymbol.Vector3),
                    new ParameterSymbol("damping", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.AngularSpring))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol LinearMotor
                = new BuiltinFunctionSymbol("linearMotor",
                [
                   new ParameterSymbol("constraint", TypeSymbol.Constraint),
                    new ParameterSymbol("speed", TypeSymbol.Vector3),
                    new ParameterSymbol("force", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Physics.LinearMotor))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol AngularMotor
                = new BuiltinFunctionSymbol("angularMotor",
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
            public static readonly FunctionSymbol Joystick
                = new BuiltinFunctionSymbol("joystick",
                [
                    new ParameterSymbol("joyDir", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("JOYSTICK_TYPE", TypeSymbol.Float),
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
            public static readonly FunctionSymbol Pow
                = new BuiltinFunctionSymbol("pow",
                [
                    new ParameterSymbol("base", TypeSymbol.Float),
                    new ParameterSymbol("exponent", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Power));
            public static readonly FunctionSymbol Random
                = new BuiltinFunctionSymbol("random",
                [
                    new ParameterSymbol("min", TypeSymbol.Float),
                    new ParameterSymbol("max", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Random));
            public static readonly FunctionSymbol RandomSeed
                = new BuiltinFunctionSymbol("setRandomSeed",
                [
                    new ParameterSymbol("seed", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Math.RandomSeed));
            public static readonly FunctionSymbol Min
                = new BuiltinFunctionSymbol("min",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Min));
            public static readonly FunctionSymbol Max
                = new BuiltinFunctionSymbol("max",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Max));
            public static readonly FunctionSymbol Sin
                = new BuiltinFunctionSymbol("sin",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Sin));
            public static readonly FunctionSymbol Cos
                = new BuiltinFunctionSymbol("cos",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Cos));
            public static readonly FunctionSymbol Round
                = new BuiltinFunctionSymbol("round",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Round));
            public static readonly FunctionSymbol Floor
                = new BuiltinFunctionSymbol("floor",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Floor));
            public static readonly FunctionSymbol Ceiling
                = new BuiltinFunctionSymbol("ceiling",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Ceiling));
            public static readonly FunctionSymbol Abs
                = new BuiltinFunctionSymbol("abs",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Absolute));
            public static readonly FunctionSymbol Log
                = new BuiltinFunctionSymbol("log",
                [
                    new ParameterSymbol("number", TypeSymbol.Float),
                    new ParameterSymbol("base", TypeSymbol.Float),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Logarithm));
            public static readonly FunctionSymbol Normalize
                = new BuiltinFunctionSymbol("normalize",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.Normalize));
            public static readonly FunctionSymbol DotProduct
                = new BuiltinFunctionSymbol("dot",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.DotProduct));
            public static readonly FunctionSymbol CrossProduct
                = new BuiltinFunctionSymbol("cross",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.CrossProduct));
            public static readonly FunctionSymbol Distance
                = new BuiltinFunctionSymbol("dist",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Distance));
            public static readonly FunctionSymbol Lerp
                = new BuiltinFunctionSymbol("lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Rotation),
                    new ParameterSymbol("to", TypeSymbol.Rotation),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.Lerp));
            public static readonly FunctionSymbol LerpFloat
                = new BuiltinFunctionSymbol("lerp",
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
            public static readonly FunctionSymbol LerpVec
                = new BuiltinFunctionSymbol("lerp",
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
            public static readonly FunctionSymbol AxisAngle
                = new BuiltinFunctionSymbol("axisAngle",
                [
                    new ParameterSymbol("axis", TypeSymbol.Vector3),
                    new ParameterSymbol("angle", TypeSymbol.Float),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.AxisAngle));
            public static readonly FunctionSymbol ScreenToWorld
                = new BuiltinFunctionSymbol("screenToWorld",
                [
                    new ParameterSymbol("screenX", TypeSymbol.Float),
                    new ParameterSymbol("screenY", TypeSymbol.Float),
                    new ParameterSymbol("worldNear", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("worldFar", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.ScreenToWorld));
            public static readonly FunctionSymbol WorldToScreen
                = new BuiltinFunctionSymbol("worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3),
                    new ParameterSymbol("screenX", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("screenY", Modifiers.Out, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.WorldToScreen));
            public static readonly FunctionSymbol WorldToScreen2
                = new BuiltinFunctionSymbol("worldToScreen",
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
            public static readonly FunctionSymbol LookRotation
                = new BuiltinFunctionSymbol("lookRotation",
                [
                    new ParameterSymbol("direction", TypeSymbol.Vector3),
                    new ParameterSymbol("up", TypeSymbol.Vector3),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.LookRotation));
            public static readonly FunctionSymbol LineVsPlane
                = new BuiltinFunctionSymbol("lineVsPlane",
                [
                    new ParameterSymbol("lineFrom", TypeSymbol.Vector3),
                    new ParameterSymbol("lineTo", TypeSymbol.Vector3),
                    new ParameterSymbol("planePoint", TypeSymbol.Vector3),
                    new ParameterSymbol("planeNormal", TypeSymbol.Vector3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.LineVsPlane));
        }

        public static readonly FunctionSymbol Inspect
            = new BuiltinFunctionSymbol("inspect",
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
        public static readonly FunctionSymbol Array_Get
            = new BuiltinFunctionSymbol("get",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
            ], TypeSymbol.Generic, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
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
        public static readonly FunctionSymbol Array_Set
            = new BuiltinFunctionSymbol("set",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
                new ParameterSymbol("value", TypeSymbol.Generic),
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
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
        public static readonly FunctionSymbol Array_SetRange
            = new BuiltinFunctionSymbol("setRange",
            [
                new ParameterSymbol("array", TypeSymbol.Array),
                new ParameterSymbol("index", TypeSymbol.Float),
                new ParameterSymbol("value", TypeSymbol.ArraySegment),
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(1..2), true);
                if (constants is null)
                    return NopEmitStore.Instance;

                EmitStore firstStore = null!;
                EmitStore lastStore = null!;

                var segment = (BoundArraySegmentExpression)call.Arguments[2];

                Debug.Assert(segment.Elements.Length > 0);

                float offset = (float?)constants[0] ?? 0f;

                for (int i = 0; i < segment.Elements.Length; i++)
                {
                    EmitStore store = ((BuiltinFunctionSymbol)Array_Set)
                        .Emit(
                            new BoundCallExpression(
                                call.Syntax,
                                Array_Set,
                                new BoundArgumentClause(call.ArgumentClause.Syntax, [0, 0, 0],
                                    [
                                        call.Arguments[0],
                                        new BoundLiteralExpression(call.Syntax, i + offset),
                                        segment.Elements[i],
                                    ]),
                                TypeSymbol.Void,
                                call.GenericType
                            ),
                            context
                        );

                    firstStore ??= store;
                    lastStore = store;
                }

                return new MultiEmitStore(firstStore, lastStore);
            })
            {
                IsMethod = true,
            };
        public static readonly FunctionSymbol Ptr_SetValue
            = new BuiltinFunctionSymbol("setPtrValue",
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
        public static readonly FunctionSymbol FcComment
            = new BuiltinFunctionSymbol("fcComment",
            [
                new ParameterSymbol("TEXT", TypeSymbol.String)
            ], TypeSymbol.Void, (call, context) =>
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                if (constants is not null && constants[0] is string text && !string.IsNullOrEmpty(text))
                    context.WriteComment(text);

                return NopEmitStore.Instance;
            }
            );
        public static readonly FunctionSymbol GetBlockById
            = new BuiltinFunctionSymbol("getBlockById",
            [
                new ParameterSymbol("BLOCK", TypeSymbol.Float)
            ], TypeSymbol.Object, (call, context) =>
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                if (constants is null)
                    return NopEmitStore.Instance;

                float _id = (constants[0] as float?) ?? 0f;
                ushort id = (ushort)System.Math.Clamp((int)_id, ushort.MinValue, ushort.MaxValue);

                // TODO: somehow calculate the size, for now just use the largest stock one
                Block block = context.AddBlock(new BlockDef(string.Empty, id, BlockType.NonScript, new Vector2I(2, 4)));

                if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                {
                    context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);
                    return NopEmitStore.Instance;
                }

                return new BasicEmitStore(new NopConnectTarget(), [new BlockVoxelConnectTarget(block)]);
            }
            );

        public static readonly FunctionSymbol ToRot
           = new BuiltinFunctionSymbol("toRot",
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
        public static readonly FunctionSymbol ToVec
           = new BuiltinFunctionSymbol("toVec",
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
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol)f.GetValue(null)!)
            )
            .Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol)f.GetValue(null)!)
            );

        private static IEnumerable<(FunctionSymbol, string?)>? functionsWithCategoryCache;
        internal static IEnumerable<(FunctionSymbol, string?)> GetAllWithCategory()
            => functionsWithCategoryCache ??= typeof(BuiltinFunctions)
            .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static)
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => ((FunctionSymbol)f.GetValue(null)!, (string?)type.Name))
            )
            .Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => ((FunctionSymbol)f.GetValue(null)!, (string?)null))
            );
    }
}
