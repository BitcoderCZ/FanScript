using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Reflection;

namespace FanScript.Compiler.Symbols
{
    internal static class BuiltinFunctions
    {
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

        private static IEnumerable<FunctionSymbol>? functionsCache;
        internal static IEnumerable<FunctionSymbol> GetAll()
            => functionsCache ??= typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
                                       .Where(f => f.FieldType == typeof(FunctionSymbol))
                                       .Select(f => (FunctionSymbol)f.GetValue(null)!);
    }
}
