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
        private static class Math
        {
            private static EmitStore emit10(BoundCallExpression call, EmitContext context, BlockDef blockDef)
            {
                Block block = context.Builder.AddBlock(blockDef);

                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore num = context.EmitExpression(call.Arguments[0]);

                    context.Connect(num, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return new BasicEmitStore(block);
            }
            private static EmitStore emit11(BoundCallExpression call, EmitContext context, Func<float, float>? constant, BlockDef blockDef)
                => emit11(call, context, constant, blockDef, Blocks.Values.Number);
            private static EmitStore emit11<T>(BoundCallExpression call, EmitContext context, Func<T, T>? constant, BlockDef blockDef, BlockDef literalDef)
                where T : notnull
            {
                object[]? constants = context.ValidateConstants(call.Arguments, false);
                if (constants is not null && constant is not null)
                {
                    Block literal = context.Builder.AddBlock(literalDef);

                    context.Builder.SetBlockValue(literal, 0, constant((T)constants[0]));

                    return BasicEmitStore.COut(literal, literal.Type.Terminals[0]);
                }

                Block block = context.Builder.AddBlock(blockDef);

                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore num = context.EmitExpression(call.Arguments[0]);

                    context.Connect(num, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }
            private static EmitStore emit21(BoundCallExpression call, EmitContext context, Func<float, float, float>? constant, BlockDef blockDef)
                => emit21(call, context, constant, blockDef, Blocks.Values.Number);
            private static EmitStore emit21<T1, T2, TOut>(BoundCallExpression call, EmitContext context, Func<T1, T2, TOut>? constant, BlockDef blockDef, BlockDef literalDef)
                where T1 : notnull
                where T2 : notnull
                where TOut : notnull
            {
                object[]? constants = context.ValidateConstants(call.Arguments, false);
                if (constants is not null && constant is not null)
                {
                    Block literal = context.Builder.AddBlock(literalDef);

                    context.Builder.SetBlockValue(literal, 0, constant((T1)constants[0], (T2)constants[1]));

                    return BasicEmitStore.COut(literal, literal.Type.Terminals[0]);
                }

                Block block = context.Builder.AddBlock(blockDef);

                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore num1 = context.EmitExpression(call.Arguments[0]);
                    EmitStore num2 = context.EmitExpression(call.Arguments[1]);

                    context.Connect(num1, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    context.Connect(num2, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }
            private static EmitStore emit31<T1, T2, T3, TOut>(BoundCallExpression call, EmitContext context, Func<T1, T2, T3, TOut>? constant, BlockDef blockDef, BlockDef literalDef)
                where T1 : notnull
                where T2 : notnull
                where T3 : notnull
                where TOut : notnull
            {
                object[]? constants = context.ValidateConstants(call.Arguments, false);
                if (constants is not null && constant is not null)
                {
                    Block literal = context.Builder.AddBlock(literalDef);

                    context.Builder.SetBlockValue(literal, 0, constant((T1)constants[0], (T2)constants[1], (T3)constants[2]));

                    return BasicEmitStore.COut(literal, literal.Type.Terminals[0]);
                }

                Block block = context.Builder.AddBlock(blockDef);

                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore num1 = context.EmitExpression(call.Arguments[0]);
                    EmitStore num2 = context.EmitExpression(call.Arguments[1]);
                    EmitStore num3 = context.EmitExpression(call.Arguments[2]);

                    context.Connect(num1, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                    context.Connect(num2, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    context.Connect(num3, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }
            private static EmitStore emit41<T1, T2, T3, T4, TOut>(BoundCallExpression call, EmitContext context, Func<T1, T2, T3, T4, TOut>? constant, BlockDef blockDef, BlockDef literalDef)
                where T1 : notnull
                where T2 : notnull
                where T3 : notnull
                where T4 : notnull
                where TOut : notnull
            {
                object[]? constants = context.ValidateConstants(call.Arguments, false);
                if (constants is not null && constant is not null)
                {
                    Block literal = context.Builder.AddBlock(literalDef);

                    context.Builder.SetBlockValue(literal, 0, constant((T1)constants[0], (T2)constants[1], (T3)constants[2], (T4)constants[3]));

                    return BasicEmitStore.COut(literal, literal.Type.Terminals[0]);
                }

                Block block = context.Builder.AddBlock(blockDef);

                context.Builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore num1 = context.EmitExpression(call.Arguments[0]);
                    EmitStore num2 = context.EmitExpression(call.Arguments[1]);
                    EmitStore num3 = context.EmitExpression(call.Arguments[2]);
                    EmitStore num4 = context.EmitExpression(call.Arguments[3]);

                    context.Connect(num1, BasicEmitStore.CIn(block, block.Type.Terminals[4]));
                    context.Connect(num2, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                    context.Connect(num3, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    context.Connect(num4, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }

            public static readonly FunctionSymbol Random
                = new BuiltinFunctionSymbol("random",
                [
                    new ParameterSymbol("min", TypeSymbol.Float, 0),
                    new ParameterSymbol("max", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emit21(call, context, null, Blocks.Math.Random));
            public static readonly FunctionSymbol RandomSeed
                = new BuiltinFunctionSymbol("setRandomSeed",
                [
                    new ParameterSymbol("seed", TypeSymbol.Float, 0),
                ], TypeSymbol.Void, (call, context) => emit10(call, context, Blocks.Math.RandomSeed));
            public static readonly FunctionSymbol Min
                = new BuiltinFunctionSymbol("min",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float, 0),
                    new ParameterSymbol("num2", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emit21(call, context, (num1, num2) => MathF.Min(num1, num2), Blocks.Math.Min));
            public static readonly FunctionSymbol Max
                = new BuiltinFunctionSymbol("max",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float, 0),
                    new ParameterSymbol("num2", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emit21(call, context, (num1, num2) => MathF.Max(num1, num2), Blocks.Math.Max));
            public static readonly FunctionSymbol Sin
                = new BuiltinFunctionSymbol("sin",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Sin(num), Blocks.Math.Sin));
            public static readonly FunctionSymbol Cos
                = new BuiltinFunctionSymbol("cos",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Cos(num), Blocks.Math.Cos));
            public static readonly FunctionSymbol Round
                = new BuiltinFunctionSymbol("round",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Round(num), Blocks.Math.Round));
            public static readonly FunctionSymbol Floor
                = new BuiltinFunctionSymbol("floor",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Floor(num), Blocks.Math.Floor));
            public static readonly FunctionSymbol Ceiling
                = new BuiltinFunctionSymbol("ceiling",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Ceiling(num), Blocks.Math.Ceiling));
            public static readonly FunctionSymbol Abs
                = new BuiltinFunctionSymbol("abs",
                [
                    new ParameterSymbol("num", TypeSymbol.Float, 0),
                ], TypeSymbol.Float, (call, context) => emit11(call, context, num => MathF.Abs(num), Blocks.Math.Absolute));
            public static readonly FunctionSymbol Log
                = new BuiltinFunctionSymbol("log",
                [
                    new ParameterSymbol("number", TypeSymbol.Float, 0),
                    new ParameterSymbol("base", TypeSymbol.Float, 1),
                ], TypeSymbol.Float, (call, context) => emit21(call, context, (number, @base) => MathF.Log(number, @base), Blocks.Math.Logarithm));
            public static readonly FunctionSymbol Normalize
                = new BuiltinFunctionSymbol("normalize",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                ], TypeSymbol.Vector3, (call, context) => emit11<Vector3F>(call, context, vec => vec.Normalized(), Blocks.Math.Normalize, Blocks.Values.Vector));
            public static readonly FunctionSymbol DotProduct
                = new BuiltinFunctionSymbol("dot",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Float, (call, context) => emit21<Vector3F, Vector3F, float>(call, context, (vec1, vec2) => Vector3F.Dot(vec1, vec2), Blocks.Math.DotProduct, Blocks.Values.Number));
            public static readonly FunctionSymbol CrossProduct
                = new BuiltinFunctionSymbol("cross",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Vector3, (call, context) => emit21<Vector3F, Vector3F, Vector3F>(call, context, (vec1, vec2) => Vector3F.Cross(vec1, vec2), Blocks.Math.CrossProduct, Blocks.Values.Vector));
            public static readonly FunctionSymbol Distance
                = new BuiltinFunctionSymbol("dist",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("vector", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Float, (call, context) => emit21<Vector3F, Vector3F, float>(call, context, (vec1, vec2) => (float)Vector3F.Distance(vec1, vec2), Blocks.Math.Distance, Blocks.Values.Number));
            public static readonly FunctionSymbol Lerp
                = new BuiltinFunctionSymbol("lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Rotation, 0),
                    new ParameterSymbol("to", TypeSymbol.Rotation, 1),
                    new ParameterSymbol("amount", TypeSymbol.Float, 2),
                ], TypeSymbol.Rotation, (call, context) => emit31<Rotation, Rotation, float, Rotation>(call, context, null, Blocks.Math.Lerp, Blocks.Values.Rotation));
            public static readonly FunctionSymbol AxisAngle
                = new BuiltinFunctionSymbol("axisAngle",
                [
                    new ParameterSymbol("axis", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("angle", TypeSymbol.Float, 1),
                ], TypeSymbol.Rotation, (call, context) => emit21<Vector3F, float, Rotation>(call, context, null, Blocks.Math.AxisAngle, Blocks.Values.Rotation));
            public static readonly FunctionSymbol LookRotation
                = new BuiltinFunctionSymbol("lookRotation",
                [
                    new ParameterSymbol("direction", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("up", TypeSymbol.Vector3, 1),
                ], TypeSymbol.Rotation, (call, context) => emit21<Vector3F, Vector3F, Rotation>(call, context, null, Blocks.Math.LookRotation, Blocks.Values.Rotation));
            public static readonly FunctionSymbol LineVsPlane
                = new BuiltinFunctionSymbol("lineVsPlane",
                [
                    new ParameterSymbol("lineFrom", TypeSymbol.Vector3, 0),
                    new ParameterSymbol("lineTo", TypeSymbol.Vector3, 1),
                    new ParameterSymbol("planePoint", TypeSymbol.Vector3, 2),
                    new ParameterSymbol("planeNormal", TypeSymbol.Vector3, 3),
                ], TypeSymbol.Vector3, (call, context) => emit41<Vector3F, Vector3F, Vector3F, Vector3F, Vector3F>(call, context, null, Blocks.Math.LineVsPlane, Blocks.Values.Vector));
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

                  object[]? args = context.ValidateConstants(call.Arguments, true);
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
