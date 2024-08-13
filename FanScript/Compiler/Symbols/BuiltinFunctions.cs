using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler.Symbols
{
    internal static class BuiltinFunctions
    {
        // (A) - active block (has before and after), num - numb inputs, num - number outputs
        private static EmitStore emitAX0(BoundCallExpression call, EmitContext context, BlockDef blockDef, int argumentOffset = 0, Type[]? constantTypes = null)
        {
            constantTypes ??= [];

            Block block = context.Builder.AddBlock(blockDef);

            int argLength = call.Arguments.Length - constantTypes.Length;
            Debug.Assert(argLength >= 0);

            if (argLength != 0)
                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    for (int i = 0; i < argLength; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[argLength - i + argumentOffset]));
                });

            if (constantTypes.Length != 0)
            {
                object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(^constantTypes.Length..), true);

                if (constants is null)
                    return new NopEmitStore();

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

                    context.Builder.SetBlockValue(block, i, val);
                }
            }

            return new BasicEmitStore(block);
        }
        /// <summary>
        /// TODO: if used with anything differend than 11, test
        /// </summary>
        private static EmitStore emitAXX(BoundCallExpression call, EmitContext context, int numbReturnArgs, BlockDef blockDef)
        {
            if (numbReturnArgs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
            else if (numbReturnArgs > call.Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));

            int retArgStart = call.Arguments.Length - numbReturnArgs;

            Block? block = context.Builder.AddBlock(blockDef);

            EmitStore lastStore = BasicEmitStore.COut(block);

            if (retArgStart != 0)
                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    for (int i = 0; i < retArgStart; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[retArgStart - i + numbReturnArgs]));
                });

            context.Builder.BlockPlacer.StatementBlock(() =>
            {
                for (int i = retArgStart; i < call.Arguments.Length; i++)
                {
                    VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

                    EmitStore varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.Terminals[call.Arguments.Length - i]));

                    context.Connect(lastStore, varStore);

                    if (varStore is not NopEmitStore)
                        lastStore = varStore;
                }
            });

            return new MultiEmitStore(BasicEmitStore.CIn(block), lastStore);
        }
        private static EmitStore emitXX(BoundCallExpression call, EmitContext context, int numbReturnArgs, BlockDef blockDef)
        {
            if (numbReturnArgs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
            else if (numbReturnArgs > call.Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));

            int retArgStart = call.Arguments.Length - numbReturnArgs;

            EmitStore inStore = null!;
            EmitStore lastStore = null!;

            Block? block = null;

            for (int i = retArgStart; i < call.Arguments.Length; i++)
            {
                VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

                EmitStore varStore;

                if (block is null)
                {
                    block = null!;
                    varStore = context.EmitSetVariable(variable, () =>
                    {
                        context.Builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            block = context.Builder.AddBlock(blockDef);

                            if (retArgStart != 0)
                                context.Builder.BlockPlacer.ExpressionBlock(() =>
                                {
                                    for (int i = 0; i < retArgStart; i++)
                                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[(retArgStart - 1) - i + numbReturnArgs]));
                                });
                        });

                        return BasicEmitStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]);
                    });
                }
                else
                {
                    varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]));

                    context.Connect(lastStore, varStore);
                }

                if (varStore is not NopEmitStore)
                {
                    if (inStore is null)
                        inStore = varStore;

                    lastStore = varStore;
                }
            }

            return new MultiEmitStore(inStore, lastStore);
        }
        private static EmitStore emitX1(BoundCallExpression call, EmitContext context, BlockDef blockDef)
        {
            Block block = context.Builder.AddBlock(blockDef);

            if (call.Arguments.Length != 0)
                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    for (int i = 0; i < call.Arguments.Length; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[call.Arguments.Length - i]));
                });

            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private static class Game
        {
            public static readonly FunctionSymbol Win
                = new BuiltinFunctionSymbol("win", [
                    new ParameterSymbol("DELAY", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Win, constantTypes: [typeof(byte)]));
            public static readonly FunctionSymbol Lose
                = new BuiltinFunctionSymbol("lose", [
                    new ParameterSymbol("DELAY", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.Lose, constantTypes: [typeof(byte)]));
            public static readonly FunctionSymbol SetScore
                = new BuiltinFunctionSymbol("setScore", [
                    new ParameterSymbol("score", TypeSymbol.Float),
                    new ParameterSymbol("coins", TypeSymbol.Float),
                    new ParameterSymbol("RANKING", TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetScore, constantTypes: [typeof(byte)]));
            public static readonly FunctionSymbol SetCamera
                = new BuiltinFunctionSymbol("setCamera", [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                    new ParameterSymbol("range", TypeSymbol.Float),
                    new ParameterSymbol("PERSPECTIVE", TypeSymbol.Bool),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetCamera, constantTypes: [typeof(byte)]));
            public static readonly FunctionSymbol SetLight
                = new BuiltinFunctionSymbol("setLight", [
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Game.SetLight));
            public static readonly FunctionSymbol GetScreenSize
                = new BuiltinFunctionSymbol("getScreenSize", [
                    new ParameterSymbol("width", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("height", Modifiers.Out, TypeSymbol.Float),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Game.ScreenSize));
            public static readonly FunctionSymbol GetScreenSize2
               = new BuiltinFunctionSymbol("getScreenSize", [], TypeSymbol.Vector3, (call, context) =>
               {
                   Block make = context.Builder.AddBlock(Blocks.Math.Make_Vector);

                   context.Builder.BlockPlacer.ExpressionBlock(() =>
                   {
                       Block ss = context.Builder.AddBlock(Blocks.Game.ScreenSize);

                       context.Connect(BasicEmitStore.COut(ss, ss.Type.Terminals[1]), BasicEmitStore.CIn(make, make.Type.Terminals[3]));
                       context.Connect(BasicEmitStore.COut(ss, ss.Type.Terminals[0]), BasicEmitStore.CIn(make, make.Type.Terminals[2]));
                   });

                   return BasicEmitStore.COut(make, make.Type.Terminals[0]);
               });
            public static readonly FunctionSymbol GetAccelerometer
                = new BuiltinFunctionSymbol("getAccelerometer", [], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Game.Accelerometer));
            public static readonly FunctionSymbol GetCurrentFrame
                = new BuiltinFunctionSymbol("getCurrentFrame", [], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Game.CurrentFrame));
            public static readonly FunctionSymbol ShopSection
                = new BuiltinFunctionSymbol("shopSection", [
                    new ParameterSymbol("NAME", TypeSymbol.String),
                ], TypeSymbol.Void, (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

                    if (constants is null)
                        return new NopEmitStore();

                    Block block = context.Builder.AddBlock(Blocks.Game.MenuItem);

                    context.Builder.SetBlockValue(block, 0, constants[0] ?? string.Empty);

                    return new BasicEmitStore(block);
                });
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
            public static readonly FunctionSymbol GetObject
          = new BuiltinFunctionSymbol("getObject", [
              new ParameterSymbol("position", TypeSymbol.Vector3),
            ], TypeSymbol.Object, (call, context) =>
            {
                BoundConstant? constant = call.Arguments[0].ConstantValue;
                if (constant is null)
                {
                    context.Diagnostics.ReportValueMustBeConstant(call.Arguments[0].Syntax.Location);
                    return new NopEmitStore();
                }

                Vector3I pos = (Vector3I)((Vector3F)constant.GetValueOrDefault(TypeSymbol.Vector3)); // unbox, then cast

                if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                {
                    context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);
                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");
                    });
                    return new NopEmitStore();
                }

                return new AbsoluteEmitStore(pos, null);
            }
          );
            public static readonly FunctionSymbol GetObject2
              = new BuiltinFunctionSymbol("getObject", [
                  new ParameterSymbol("x", TypeSymbol.Float),
                  new ParameterSymbol("y", TypeSymbol.Float),
                  new ParameterSymbol("z", TypeSymbol.Float),
                ], TypeSymbol.Object, (call, context) =>
                {
                    object?[]? args = context.ValidateConstants(call.Arguments.AsMemory(), true);
                    if (args is null)
                        return new NopEmitStore();

                    Vector3I pos = new Vector3I((int)((float?)args[0] ?? 0f), (int)((float?)args[1] ?? 0f), (int)((float?)args[2] ?? 0f)); // unbox, then cast

                    if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                    {
                        context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);
                        context.Builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");
                        });
                        return new NopEmitStore();
                    }

                    return new AbsoluteEmitStore(pos, null);
                }
              );
            public static readonly FunctionSymbol SetPos
                = new BuiltinFunctionSymbol("setPos", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Objects.SetPos, argumentOffset: 1))
                {
                    IsMethod = true,
                };
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
            public static readonly FunctionSymbol GetPos
                = new BuiltinFunctionSymbol("getPos", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", Modifiers.Out, TypeSymbol.Rotation),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Objects.GetPos))
                {
                    IsMethod = true,
                };
            public static readonly FunctionSymbol Raycast
                = new BuiltinFunctionSymbol("raycast", [
                    new ParameterSymbol("from", TypeSymbol.Vector3),
                    new ParameterSymbol("to", TypeSymbol.Vector3),
                    new ParameterSymbol("didHit", Modifiers.Out, TypeSymbol.Bool),
                    new ParameterSymbol("hitPos", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("hitObj", Modifiers.Out, TypeSymbol.Object),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 3, Blocks.Objects.Raycast));
            public static readonly FunctionSymbol GetSize
                = new BuiltinFunctionSymbol("getSize", [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("min", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("max", Modifiers.Out, TypeSymbol.Vector3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Objects.GetSize))
                {
                    IsMethod = true,
                };
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
                      return new NopEmitStore();

                  Block playSound = context.Builder.AddBlock(Blocks.Sound.PlaySound);

                  context.Builder.SetBlockValue(playSound, 0, (byte)(((bool?)values[0] ?? false) ? 1 : 0)); // loop
                  context.Builder.SetBlockValue(playSound, 1, (ushort)((float?)values[1] ?? 0f)); // sound

                  context.Builder.BlockPlacer.ExpressionBlock(() =>
                  {
                      EmitStore volume = context.EmitExpression(call.Arguments[0]);
                      EmitStore pitch = context.EmitExpression(call.Arguments[1]);

                      context.Connect(volume, BasicEmitStore.CIn(playSound, playSound.Type.Terminals[3]));
                      context.Connect(pitch, BasicEmitStore.CIn(playSound, playSound.Type.Terminals[2]));
                  });

                  EmitStore varStore = null!;
                  context.Builder.BlockPlacer.StatementBlock(() =>
                  {
                      VariableSymbol variable = ((BoundVariableExpression)call.Arguments[2]).Variable;

                      varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(playSound, playSound.Type.Terminals[1]));

                      context.Connect(BasicEmitStore.COut(playSound), varStore);
                  });

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
                        return new NopEmitStore();

                    Block joystick = context.Builder.AddBlock(Blocks.Control.Joystick);

                    context.Builder.SetBlockValue(joystick, 0, (byte)((float?)values[0] ?? 0f)); // unbox, then cast

                    EmitStore varStore = null!;
                    context.Builder.BlockPlacer.StatementBlock(() =>
                    {
                        VariableSymbol variable = ((BoundVariableExpression)call.Arguments[0]).Variable;

                        varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(joystick, joystick.Type.Terminals[1]));

                        context.Connect(BasicEmitStore.COut(joystick), varStore);
                    });

                    return new MultiEmitStore(BasicEmitStore.CIn(joystick), varStore is NopEmitStore ? BasicEmitStore.COut(joystick) : varStore);
                });
        }

        private static class Math
        {
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
                    Block make = context.Builder.AddBlock(Blocks.Math.Make_Vector);

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        Block wts = context.Builder.AddBlock(Blocks.Math.WorldToScreen);

                        context.Builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            EmitStore store = context.EmitExpression(call.Arguments[0]);
                            context.Connect(store, BasicEmitStore.CIn(wts, wts.Type.Terminals[2]));
                        });

                        context.Connect(BasicEmitStore.COut(wts, wts.Type.Terminals[1]), BasicEmitStore.CIn(make, make.Type.Terminals[3]));
                        context.Connect(BasicEmitStore.COut(wts, wts.Type.Terminals[0]), BasicEmitStore.CIn(make, make.Type.Terminals[2]));
                    });

                    return BasicEmitStore.COut(make, make.Type.Terminals[0]);
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
            ], TypeSymbol.Void, TypeSymbol.BuiltInNonGenericTypes, (call, context) =>
                {
                    Block inspect = context.Builder.AddBlock(Blocks.Values.InspectByType(call.GenericType!.ToWireType()));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore store = context.EmitExpression(call.Arguments[0]);

                        context.Connect(store, BasicEmitStore.CIn(inspect, inspect.Type.Terminals[1]));
                    });

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
                    Block list = context.Builder.AddBlock(Blocks.Variables.ListByType(call.GenericType!.ToWireType()));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore array = context.EmitExpression(call.Arguments[0]);
                        EmitStore index = context.EmitExpression(call.Arguments[1]);

                        context.Connect(array, BasicEmitStore.CIn(list, list.Type.Terminals[2]));
                        context.Connect(index, BasicEmitStore.CIn(list, list.Type.Terminals[1]));
                    });

                    return BasicEmitStore.COut(list, list.Type.Terminals[0]);
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
                    Block setPtr = context.Builder.AddBlock(Blocks.Variables.Set_PtrByType(call.GenericType!.ToWireType()));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        Block list = context.Builder.AddBlock(Blocks.Variables.ListByType(call.GenericType!.ToWireType()));

                        context.Builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            EmitStore array = context.EmitExpression(call.Arguments[0]);
                            EmitStore index = context.EmitExpression(call.Arguments[1]);

                            context.Connect(array, BasicEmitStore.CIn(list, list.Type.Terminals[2]));
                            context.Connect(index, BasicEmitStore.CIn(list, list.Type.Terminals[1]));
                        });

                        EmitStore value = context.EmitExpression(call.Arguments[2]);

                        context.Connect(BasicEmitStore.COut(list, list.Type.Terminals[0]), BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals[2]));
                        context.Connect(value, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals[1]));
                    });

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
                    return new NopEmitStore();

                EmitStore firstStore = null!;
                EmitStore lastStore = null!;

                var segment = (BoundArraySegmentExpression)call.Arguments[2];

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
                    Block setPtr = context.Builder.AddBlock(Blocks.Variables.Set_PtrByType(call.GenericType!.ToWireType()));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore ptr = context.EmitExpression(call.Arguments[0]);
                        EmitStore value = context.EmitExpression(call.Arguments[1]);

                        context.Connect(ptr, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals[2]));
                        context.Connect(value, BasicEmitStore.CIn(setPtr, setPtr.Type.Terminals[1]));
                    });

                    return new BasicEmitStore(setPtr);

                }
            );

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
    }
}
