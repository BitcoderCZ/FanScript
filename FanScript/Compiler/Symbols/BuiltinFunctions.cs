using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Reflection;

namespace FanScript.Compiler.Symbols
{
    internal static class BuiltinFunctions
    {
        // (A) - active block (has before and after), num - numb inputs, num - number outputs
        private static EmitStore emitAX0(BoundCallExpression call, EmitContext context, BlockDef blockDef)
        {
            Block block = context.Builder.AddBlock(blockDef);

            if (call.Arguments.Length != 0)
                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    for (int i = 0; i < call.Arguments.Length; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[call.Arguments.Length - i]));
                });

            return new BasicEmitStore(block);
        }
        private static EmitStore emitAXX(BoundCallExpression call, EmitContext context, int numbReturnArgs, BlockDef blockDef)
        {
            if (numbReturnArgs <= 0)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
            else if (numbReturnArgs > call.Arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));

            int retArgStart = call.Arguments.Length - numbReturnArgs;

            EmitStore lastStore = null!;

            Block? block = context.Builder.AddBlock(blockDef);

            if (retArgStart != 0)
                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    for (int i = 0; i < retArgStart; i++)
                        context.Connect(context.EmitExpression(call.Arguments[i]), BasicEmitStore.CIn(block, block.Type.Terminals[(retArgStart - 1) - i + numbReturnArgs]));
                });

            context.Builder.BlockPlacer.StatementBlock(() =>
            {
                for (int i = retArgStart; i < call.Arguments.Length; i++)
                {
                    VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;
                    Block varBlock = context.Builder.AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                    context.Builder.SetBlockValue(varBlock, 0, variable.Name);

                    if (i == 0)
                        context.Connect(BasicEmitStore.COut(block), BasicEmitStore.CIn(varBlock));
                    else
                        context.Connect(lastStore, BasicEmitStore.CIn(varBlock));

                    context.Connect(
                        BasicEmitStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]),
                        BasicEmitStore.CIn(varBlock, varBlock.Type.Terminals[1])
                    );

                    lastStore = BasicEmitStore.COut(varBlock);
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
                Block varBlock = context.Builder.AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                context.Builder.SetBlockValue(varBlock, 0, variable.Name);

                if (block is null)
                {
                    inStore = BasicEmitStore.CIn(varBlock);
                    block = null!;
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
                }
                else
                    context.Connect(lastStore, BasicEmitStore.CIn(varBlock));

                context.Connect(
                    BasicEmitStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]),
                    BasicEmitStore.CIn(varBlock, varBlock.Type.Terminals[1])
                );

                lastStore = BasicEmitStore.COut(varBlock);
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

        private static class Control
        {
            public static readonly FunctionSymbol Joystick
                = new BuiltinFunctionSymbol("joystick",
                [
                    new ParameterSymbol("joyDir", Modifiers.Ref, TypeSymbol.Vector3, 0),
                    new ParameterSymbol("JOYSTICK_TYPE", TypeSymbol.Float, 1),
                ], TypeSymbol.Void, (call, context) =>
                {
                    object[]? values = context.ValidateConstants(call.Arguments.AsSpan(Range.StartAt(1)), true);
                    if (values is null)
                        return new NopEmitStore();

                    Block joystick = context.Builder.AddBlock(Blocks.Control.Joystick);

                    context.Builder.SetBlockValue(joystick, 0, (byte)(float)values[0]); // unbox, then cast

                    Block varBlock = null!;
                    context.Builder.BlockPlacer.StatementBlock(() =>
                    {
                        VariableSymbol variable = ((BoundVariableExpression)call.Arguments[0]).Variable;
                        varBlock = context.Builder.AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                        context.Builder.SetBlockValue(varBlock, 0, variable.Name);

                        context.Connect(BasicEmitStore.COut(joystick), BasicEmitStore.CIn(varBlock));
                        context.Connect(BasicEmitStore.COut(joystick, joystick.Type.Terminals[1]), BasicEmitStore.CIn(varBlock, varBlock.Type.Terminals[1]));
                    });

                    return new MultiEmitStore(BasicEmitStore.CIn(joystick), BasicEmitStore.COut(varBlock));
                });
        }

        private static class Math
        {
            public static readonly FunctionSymbol Random
                = new BuiltinFunctionSymbol("random",
                [
                    new ParameterSymbol("min", TypeSymbol.Float, 0),
                    new ParameterSymbol("max", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Random));
            public static readonly FunctionSymbol RandomSeed
                = new BuiltinFunctionSymbol("setRandomSeed",
                [
                    new ParameterSymbol("seed", TypeSymbol.Float, 0),
                ], TypeSymbol.Void, (call, context) => emitAX0(call, context, Blocks.Math.RandomSeed));
            public static readonly FunctionSymbol Min
                = new BuiltinFunctionSymbol("min",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float, 0),
                    new ParameterSymbol("num2", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Min));
            public static readonly FunctionSymbol Max
                = new BuiltinFunctionSymbol("max",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float, 0),
                    new ParameterSymbol("num2", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Max));
            public static readonly FunctionSymbol Sin
                = new BuiltinFunctionSymbol("sin",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Sin));
            public static readonly FunctionSymbol Cos
                = new BuiltinFunctionSymbol("cos",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Cos));
            public static readonly FunctionSymbol Round
                = new BuiltinFunctionSymbol("round",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Round));
            public static readonly FunctionSymbol Floor
                = new BuiltinFunctionSymbol("floor",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Floor));
            public static readonly FunctionSymbol Ceiling
                = new BuiltinFunctionSymbol("ceiling",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Ceiling));
            public static readonly FunctionSymbol Abs
                = new BuiltinFunctionSymbol("abs",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Absolute));
            public static readonly FunctionSymbol Log
                = new BuiltinFunctionSymbol("log",
                [
                    new ParameterSymbol("number", TypeSymbol.Float, 0),
                    new ParameterSymbol("base", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Logarithm));
            public static readonly FunctionSymbol Normalize
                = new BuiltinFunctionSymbol("normalize",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.Normalize));
            public static readonly FunctionSymbol DotProduct
                = new BuiltinFunctionSymbol("dot",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.DotProduct));
            public static readonly FunctionSymbol CrossProduct
                = new BuiltinFunctionSymbol("cross",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.CrossProduct));
            public static readonly FunctionSymbol Distance
                = new BuiltinFunctionSymbol("dist",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Float, (call, context) => emitX1(call, context, Blocks.Math.Distance));
            public static readonly FunctionSymbol Lerp
                = new BuiltinFunctionSymbol("lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Rotation, 0),
                    new ParameterSymbol("to", TypeSymbol.Rotation, 1),
                    new ParameterSymbol("amount", TypeSymbol.Float, 2),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.Lerp));
            public static readonly FunctionSymbol AxisAngle
                = new BuiltinFunctionSymbol("axisAngle",
                [
                    new ParameterSymbol("axis", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("angle", TypeSymbol.Float, 1),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.AxisAngle));
            public static readonly FunctionSymbol ScreenToWorld
                = new BuiltinFunctionSymbol("screenToWorld",
                [
                    new ParameterSymbol("screenX", TypeSymbol.Float, 0),
                    new ParameterSymbol("screenY", TypeSymbol.Float, 1),
                    new ParameterSymbol("worldNear", Modifiers.Ref, TypeSymbol.Vector3, 2),
                    new ParameterSymbol("worldFar", Modifiers.Ref, TypeSymbol.Vector3, 3),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.ScreenToWorld));
            public static readonly FunctionSymbol WorldToScreen
                = new BuiltinFunctionSymbol("worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("screenX", Modifiers.Ref, TypeSymbol.Float, 1),
                    new ParameterSymbol("screenY", Modifiers.Ref, TypeSymbol.Float, 2),
                ], TypeSymbol.Void, (call, context) => emitXX(call, context, 2, Blocks.Math.WorldToScreen));
            public static readonly FunctionSymbol WorldToScreen2
                = new BuiltinFunctionSymbol("worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3, 0),
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
                    new ParameterSymbol("direction", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("up", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Rotation, (call, context) => emitX1(call, context, Blocks.Math.LookRotation));
            public static readonly FunctionSymbol LineVsPlane
                = new BuiltinFunctionSymbol("lineVsPlane",
                [
                    new ParameterSymbol("lineFrom", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("lineTo", TypeSymbol.Vector3, 1),
                    new ParameterSymbol("planePoint", TypeSymbol.Vector3, 2),
                    new ParameterSymbol("planeNormal", TypeSymbol.Vector3, 3),
                ], TypeSymbol.Vector3, (call, context) => emitX1(call, context, Blocks.Math.LineVsPlane));
        }

        public static readonly FunctionSymbol Inspect
            = new BuiltinFunctionSymbol("inspect",
            [
                new ParameterSymbol("value", TypeSymbol.Generic, 0)
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
        public static readonly FunctionSymbol Object_Get
          = new BuiltinFunctionSymbol("getObject", [
              new ParameterSymbol("position", TypeSymbol.Vector3, 0)
            ], TypeSymbol.Object, (call, context) =>
              {
                  if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);
                      return new NopEmitStore();
                  }

                  BoundConstant? constant = call.Arguments[0].ConstantValue;
                  if (constant is null)
                  {
                      context.Diagnostics.ReportValueMustBeConstant(call.Arguments[0].Syntax.Location);
                      return new NopEmitStore();
                  }

                  Vector3I pos = (Vector3I)((Vector3F)constant.Value); // unbox, then cast

                  return new AbsoluteEmitStore(pos, null);
              }
          );
        public static readonly FunctionSymbol Object_Get2
          = new BuiltinFunctionSymbol("getObject", [
              new ParameterSymbol("x", TypeSymbol.Float, 0),
              new ParameterSymbol("y", TypeSymbol.Float, 1),
              new ParameterSymbol("z", TypeSymbol.Float, 2)
            ], TypeSymbol.Object, (call, context) =>
              {
                  if (!context.Builder.PlatformInfo.HasFlag(BuildPlatformInfo.CanGetBlocks))
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnPlatform(call.Syntax.Location, BuildPlatformInfo.CanGetBlocks);
                      return new NopEmitStore();
                  }

                  object[]? args = context.ValidateConstants(call.Arguments.AsSpan(), true);
                  if (args is null)
                      return new NopEmitStore();

                  Vector3I pos = new Vector3I((int)(float)args[0], (int)(float)args[1], (int)(float)args[2]); // unbox, then cast

                  return new AbsoluteEmitStore(pos, null);
              }
          );
        public static readonly FunctionSymbol Object_SetPos
            = new BuiltinFunctionSymbol("setPos", [
                new ParameterSymbol("object", TypeSymbol.Object, 0),
                new ParameterSymbol("position", TypeSymbol.Vector3, 1)
            ], TypeSymbol.Void, (call, context) =>
                {
                    Block block = context.Builder.AddBlock(Blocks.Objects.SetPos);

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore blockEmit = context.EmitExpression(call.Arguments[0]);
                        EmitStore posEmit = context.EmitExpression(call.Arguments[1]);

                        context.Connect(blockEmit, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                        context.Connect(posEmit, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    });

                    return new BasicEmitStore(block);
                }
            );
        public static readonly FunctionSymbol Object_SetPos2
            = new BuiltinFunctionSymbol("setPos",
            [
                new ParameterSymbol("object", TypeSymbol.Object, 0),
                new ParameterSymbol("position", TypeSymbol.Vector3, 1),
                new ParameterSymbol("rotation", TypeSymbol.Rotation, 2)
            ], TypeSymbol.Void, (call, context) =>
                {
                    Block block = context.Builder.AddBlock(Blocks.Objects.SetPos);

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore blockEmit = context.EmitExpression(call.Arguments[0]);
                        EmitStore posEmit = context.EmitExpression(call.Arguments[1]);
                        EmitStore rotEmit = context.EmitExpression(call.Arguments[2]);

                        context.Connect(blockEmit, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                        context.Connect(posEmit, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                        context.Connect(rotEmit, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                    });

                    return new BasicEmitStore(block);
                }
            );
        public static readonly FunctionSymbol Array_Get
            = new BuiltinFunctionSymbol("get",
            [
                new ParameterSymbol("array", TypeSymbol.Array, 0),
                new ParameterSymbol("index", TypeSymbol.Float, 1),
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
            );
        public static readonly FunctionSymbol Array_Set
            = new BuiltinFunctionSymbol("set",
            [
                new ParameterSymbol("array", TypeSymbol.Array, 0),
                new ParameterSymbol("index", TypeSymbol.Float, 1),
                new ParameterSymbol("value", TypeSymbol.Generic, 2),
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
            );
        public static readonly FunctionSymbol Ptr_SetValue
            = new BuiltinFunctionSymbol("setPtrValue",
            [
                new ParameterSymbol("pointer", TypeSymbol.Generic, 0),
                new ParameterSymbol("value", TypeSymbol.Generic, 1),
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
