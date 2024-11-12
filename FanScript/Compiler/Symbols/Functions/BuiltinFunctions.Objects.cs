using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
    private static class Objects
    {
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
            ])]
        public static readonly FunctionSymbol GetObject
          = new BuiltinFunctionSymbol(
              ObjectNamespace,
              "getObject",
              [
                  new ParameterSymbol("POSITION", Modifiers.Constant, TypeSymbol.Vector3),
              ],
              TypeSymbol.Object,
              (call, context) =>
              {
                  BoundConstant? constant = call.Arguments[0].ConstantValue;
                  if (constant is null)
                  {
                      context.Diagnostics.ReportValueMustBeConstant(call.Arguments[0].Syntax.Location);
                      return NopEmitStore.Instance;
                  }

                  Vector3I pos = (Vector3I)(Vector3F)constant.GetValueOrDefault(TypeSymbol.Vector3); // unbox, then cast

                  if (context.Builder is not IConnectToBlocksBuilder)
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);

                      using (context.ExpressionBlock())
                      {
                          context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");
                      }

                      return NopEmitStore.Instance;
                  }

                  return new AbsoluteEmitStore(pos, null);
              });

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
            ])]
        public static readonly FunctionSymbol GetObject2
          = new BuiltinFunctionSymbol(
              ObjectNamespace,
              "getObject",
              [
                  new ParameterSymbol("X", Modifiers.Constant, TypeSymbol.Float),
                  new ParameterSymbol("Y", Modifiers.Constant, TypeSymbol.Float),
                  new ParameterSymbol("Z", Modifiers.Constant, TypeSymbol.Float),
              ],
              TypeSymbol.Object,
              (call, context) =>
              {
                  object?[]? args = context.ValidateConstants(call.Arguments.AsMemory(), true);
                  if (args is null)
                  {
                      return NopEmitStore.Instance;
                  }

                  Vector3I pos = new Vector3I((int)((float?)args[0] ?? 0f), (int)((float?)args[1] ?? 0f), (int)((float?)args[2] ?? 0f)); // unbox, then cast

                  if (context.Builder is not IConnectToBlocksBuilder)
                  {
                      context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);

                      using (context.ExpressionBlock())
                      {
                          context.WriteComment($"Connect to ({pos.X}, {pos.Y}, {pos.Z})");
                      }

                      return NopEmitStore.Instance;
                  }

                  return new AbsoluteEmitStore(pos, null);
              });

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
            ])]
        public static readonly FunctionSymbol SetPos
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "setPos",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Objects.SetPos, argumentOffset: 1))
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
            ])]
        public static readonly FunctionSymbol SetPosWithRot
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "setPos",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", TypeSymbol.Rotation),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Objects.SetPos))
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
            ])]
        public static readonly FunctionSymbol GetPos
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "getPos",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("position", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("rotation", Modifiers.Out, TypeSymbol.Rotation),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 2, Blocks.Objects.GetPos))
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
            ])]
        public static readonly FunctionSymbol Raycast
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "raycast",
                [
                    new ParameterSymbol("from", TypeSymbol.Vector3),
                    new ParameterSymbol("to", TypeSymbol.Vector3),
                    new ParameterSymbol("didHit", Modifiers.Out, TypeSymbol.Bool),
                    new ParameterSymbol("hitPos", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("hitObj", Modifiers.Out, TypeSymbol.Object),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 3, Blocks.Objects.Raycast));

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
            """)]
        public static readonly FunctionSymbol GetSize
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "getSize",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("min", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("max", Modifiers.Out, TypeSymbol.Vector3),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 2, Blocks.Objects.GetSize))
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
            """)]
        public static readonly FunctionSymbol SetVisible
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "setVisible",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("visible", TypeSymbol.Bool),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Objects.SetVisible))
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
            ])]
        public static readonly FunctionSymbol Clone
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "clone",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                    new ParameterSymbol("copy", Modifiers.Out, TypeSymbol.Object),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAXX(call, context, 1, Blocks.Objects.CreateObject))
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
            ])]
        public static readonly FunctionSymbol Destroy
            = new BuiltinFunctionSymbol(
                ObjectNamespace,
                "destroy",
                [
                    new ParameterSymbol("object", TypeSymbol.Object),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Objects.DestroyObject))
            {
                IsMethod = true,
            };

        private static readonly Namespace ObjectNamespace = BuiltinNamespace + "object";
    }
}
