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
        public static readonly FunctionSymbol Inspect
            = new BuiltinFunctionSymbol("inspect",
            [
                new ParameterSymbol("value", TypeSymbol.Generic, 0)
            ], TypeSymbol.Void, [TypeSymbol.Bool, TypeSymbol.Float, TypeSymbol.Vector3, TypeSymbol.Rotation])
            {
                Emit = (call, context) =>
                {
                    Block inspect = context.Builder.AddBlock(Blocks.Values.InspectByType(call.GenericType!.ToWireType()));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore store = context.EmitExpression(call.Arguments[0]);

                        context.Connect(store, BasicEmitStore.CIn(inspect, inspect.Type.Terminals[1]));
                    });

                    return new BasicEmitStore(inspect);
                },
            };
        public static readonly FunctionSymbol Object_Get
          = new BuiltinFunctionSymbol("getObject", [
              new ParameterSymbol("position", TypeSymbol.Vector3, 0)
            ], TypeSymbol.Object)
          {
              Emit = (call, context) =>
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

                  Vector3I val = (Vector3I)((Vector3F)constant.Value); // unbox, then cast

                  return new AbsoluteEmitStore(val, null);
              }
          };
        // currently functions are resolved only by name, so this one doesn't need to be implemented yet
        public static readonly FunctionSymbol Object_Get2
          = new BuiltinFunctionSymbol("getObject", [
              new ParameterSymbol("x", TypeSymbol.Float, 0),
              new ParameterSymbol("y", TypeSymbol.Float, 1),
              new ParameterSymbol("z", TypeSymbol.Float, 2)
            ], TypeSymbol.Object);
        public static readonly FunctionSymbol Object_SetPos
            = new BuiltinFunctionSymbol("setPos", [
                new ParameterSymbol("object", TypeSymbol.Object, 0),
                new ParameterSymbol("position", TypeSymbol.Vector3, 1)
            ], TypeSymbol.Void)
            {
                Emit = (call, context) =>
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
            };
        // currently functions are resolved only by name, so this one doesn't need to be implemented yet
        public static readonly FunctionSymbol Object_SetPos2
            = new BuiltinFunctionSymbol("setPos",
            [
                new ParameterSymbol("object", TypeSymbol.Object, 0),
                new ParameterSymbol("position", TypeSymbol.Vector3, 1),
                new ParameterSymbol("rotation", TypeSymbol.Rotation, 2)
            ], TypeSymbol.Void);
        public static readonly FunctionSymbol Array_Get
            = new BuiltinFunctionSymbol("get",
            [
                new ParameterSymbol("array", TypeSymbol.Array, 0),
                new ParameterSymbol("index", TypeSymbol.Float, 1),
            ], TypeSymbol.Generic, [TypeSymbol.Bool, TypeSymbol.Float, TypeSymbol.Vector3, TypeSymbol.Rotation, TypeSymbol.Object])
            {
                Emit = (call, context) =>
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
                },
            };
        public static readonly FunctionSymbol Array_Set
            = new BuiltinFunctionSymbol("set",
            [
                new ParameterSymbol("array", TypeSymbol.Array, 0),
                new ParameterSymbol("index", TypeSymbol.Float, 1),
                new ParameterSymbol("value", TypeSymbol.Generic, 2),
            ], TypeSymbol.Void, [TypeSymbol.Bool, TypeSymbol.Float, TypeSymbol.Vector3, TypeSymbol.Rotation, TypeSymbol.Object])
            {
                Emit = (call, context) =>
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
                },
            };

        private static IEnumerable<FunctionSymbol>? functionsCache;
        internal static IEnumerable<FunctionSymbol> GetAll()
            => functionsCache ??= typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                       .Where(f => f.FieldType == typeof(FunctionSymbol))
                                       .Select(f => (FunctionSymbol)f.GetValue(null)!);
    }
}
