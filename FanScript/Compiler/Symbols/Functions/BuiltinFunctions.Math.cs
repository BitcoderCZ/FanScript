using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using MathUtils.Vectors;

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
    private static class Math
    {
        [FunctionDoc(
            Info = """
            Raises <link type="param">base</> to <link type="param">exponent</>.
            """,
            ReturnValueInfo = """
            <link type="param">base</> raised to <link type="param">exponent</>.
            """)]
        public static readonly FunctionSymbol Pow
            = new ConstantFunctionSymbol(
                MathNamespace,
                "pow",
                [
                    new ParameterSymbol("base", TypeSymbol.Float),
                    new ParameterSymbol("exponent", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Power),
                args => [MathF.Pow((float)args[0].GetValueOrDefault(TypeSymbol.Float), (float)args[1].GetValueOrDefault(TypeSymbol.Float))]);

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
            ])]
        public static readonly FunctionSymbol Random
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "random",
                [
                    new ParameterSymbol("min", TypeSymbol.Float),
                    new ParameterSymbol("max", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Random));

        [FunctionDoc(
            Info = """
            Sets the seed used by <link type="func">random;float;float</>.
            """,
            ParameterInfos = [
                """
                The new random seed.
                """,
            ])]
        public static readonly FunctionSymbol RandomSeed
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "setRandomSeed",
                [
                    new ParameterSymbol("seed", TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitAX0(call, context, Blocks.Math.RandomSeed));

        [FunctionDoc(
            Info = """
            Returns the smaller of 2 numbers.
            """,
            ReturnValueInfo = """
            Returns either <link type="param">num1</> or <link type="param">num2</>, depending on which one is smaller.
            """)]
        public static readonly FunctionSymbol Min
            = new ConstantFunctionSymbol(
                MathNamespace,
                "min",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Min),
                args => [System.Math.Min((float)args[0].GetValueOrDefault(TypeSymbol.Float), (float)args[1].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Returns the larger of 2 numbers.
            """,
            ReturnValueInfo = """
            Returns either <link type="param">num1</> or <link type="param">num2</>, depending on which one is larger.
            """)]
        public static readonly FunctionSymbol Max
            = new ConstantFunctionSymbol(
                MathNamespace,
                "max",
                [
                    new ParameterSymbol("num1", TypeSymbol.Float),
                    new ParameterSymbol("num2", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Max),
                args => [System.Math.Max((float)args[0].GetValueOrDefault(TypeSymbol.Float), (float)args[1].GetValueOrDefault(TypeSymbol.Float))]);

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
            ])]
        public static readonly FunctionSymbol Sin
            = new ConstantFunctionSymbol(
                MathNamespace,
                "sin",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Sin),
                args => [(float)System.Math.Sin(System.Math.PI * (float)args[0].GetValueOrDefault(TypeSymbol.Float) / 180.0)]);

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
            ])]
        public static readonly FunctionSymbol Cos
            = new ConstantFunctionSymbol(
                MathNamespace,
                "cos",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Cos),
                args => [(float)System.Math.Cos(System.Math.PI * (float)args[0].GetValueOrDefault(TypeSymbol.Float) / 180.0)]);

        [FunctionDoc(
            Info = """
            Returns <link type="param">num</> rounded to the nearest integer.
            """,
            ReturnValueInfo = """
            <link type="param">num</> rounded to the nearest integer.
            """)]
        public static readonly FunctionSymbol Round
            = new ConstantFunctionSymbol(
                MathNamespace,
                "round",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Round),
                args => [MathF.Round((float)args[0].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Returns <link type="param">num</> rounded down.
            """,
            ReturnValueInfo = """
            <link type="param">num</> rounded down.
            """)]
        public static readonly FunctionSymbol Floor
            = new ConstantFunctionSymbol(
                MathNamespace,
                "floor",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Floor),
                args => [MathF.Floor((float)args[0].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Returns <link type="param">num</> rounded up.
            """,
            ReturnValueInfo = """
            <link type="param">num</> rounded up.
            """)]
        public static readonly FunctionSymbol Ceiling
            = new ConstantFunctionSymbol(
                MathNamespace,
                "ceiling",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Ceiling),
                args => [MathF.Ceiling((float)args[0].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Gets the absolute value of <link type="param">num</>.
            """,
            ReturnValueInfo = """
            Absolute value of <link type="param">num</> (if (<link type="param">num</> >= 0) <link type="param">num</> else -<link type="param">num</>).
            """)]
        public static readonly FunctionSymbol Abs
            = new ConstantFunctionSymbol(
                MathNamespace,
                "abs",
                [
                    new ParameterSymbol("num", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Absolute),
                args => [MathF.Abs((float)args[0].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Returns the <link type="url">logarithm;https://en.wikipedia.org/wiki/Logarithm</> of <link type="param">number</> to <link type="param">base</>.
            """)]
        public static readonly FunctionSymbol Log
            = new ConstantFunctionSymbol(
                MathNamespace,
                "log",
                [
                    new ParameterSymbol("number", TypeSymbol.Float),
                    new ParameterSymbol("base", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Logarithm),
                args => [MathF.Log((float)args[0].GetValueOrDefault(TypeSymbol.Float), (float)args[1].GetValueOrDefault(TypeSymbol.Float))]);

        [FunctionDoc(
            Info = """
            Returns a vector with the same direction as <link type="param">vector</>, but with the lenght of 1.
            """,
            ReturnValueInfo = """
            The normalized <link type="param">vector</>.
            """)]
        public static readonly FunctionSymbol Normalize
            = new ConstantFunctionSymbol(
                MathNamespace,
                "normalize",
                [
                    new ParameterSymbol("vector", TypeSymbol.Vector3),
                ],
                TypeSymbol.Vector3,
                (call, context) => EmitX1(call, context, Blocks.Math.Normalize),
                args => [((Vector3F)args[0].GetValueOrDefault(TypeSymbol.Vector3)).Normalized()]);

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
            ])]
        public static readonly FunctionSymbol DotProduct
            = new ConstantFunctionSymbol(
                MathNamespace,
                "dot",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.DotProduct),
                args => [Vector3F.Dot((Vector3F)args[0].GetValueOrDefault(TypeSymbol.Vector3), (Vector3F)args[1].GetValueOrDefault(TypeSymbol.Vector3))]);

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
            ])]
        public static readonly FunctionSymbol CrossProduct
            = new ConstantFunctionSymbol(
                MathNamespace,
                "cross",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ],
                TypeSymbol.Vector3,
                (call, context) => EmitX1(call, context, Blocks.Math.CrossProduct),
                args => [Vector3F.Cross((Vector3F)args[0].GetValueOrDefault(TypeSymbol.Vector3), (Vector3F)args[1].GetValueOrDefault(TypeSymbol.Vector3))]);

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
            ])]
        public static readonly FunctionSymbol Distance
            = new ConstantFunctionSymbol(
                MathNamespace,
                "dist",
                [
                    new ParameterSymbol("vector1", TypeSymbol.Vector3),
                    new ParameterSymbol("vector2", TypeSymbol.Vector3),
                ],
                TypeSymbol.Float,
                (call, context) => EmitX1(call, context, Blocks.Math.Distance),
                args => [(float)Vector3F.Distance((Vector3F)args[0].GetValueOrDefault(TypeSymbol.Vector3), (Vector3F)args[1].GetValueOrDefault(TypeSymbol.Vector3))]);

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
            """)]
        public static readonly FunctionSymbol Lerp
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Rotation),
                    new ParameterSymbol("to", TypeSymbol.Rotation),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ],
                TypeSymbol.Rotation,
                (call, context) => EmitX1(call, context, Blocks.Math.Lerp));

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
            """)]
        public static readonly FunctionSymbol LerpFloat
            = new ConstantFunctionSymbol(
                MathNamespace,
                "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Float),
                    new ParameterSymbol("to", TypeSymbol.Float),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ],
                TypeSymbol.Float,
                (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

                    if (constants is not null)
                    {
                        float from = (constants[0] as float?) ?? 0f;
                        float to = (constants[1] as float?) ?? 0f;
                        float amount = (constants[2] as float?) ?? 0f;

                        Block numb = context.AddBlock(Blocks.Values.Number);
                        context.SetBlockValue(numb, 0, from + (amount * (to - from)));

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
                },
                args =>
                {
                    float from = (float)args[0].GetValueOrDefault(TypeSymbol.Float);
                    float to = (float)args[1].GetValueOrDefault(TypeSymbol.Float);
                    float amount = (float)args[2].GetValueOrDefault(TypeSymbol.Float);

                    return [
                        from + (amount * (to - from))
                    ];
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
            """)]
        public static readonly FunctionSymbol LerpVec
            = new ConstantFunctionSymbol(
                MathNamespace,
                "lerp",
                [
                    new ParameterSymbol("from", TypeSymbol.Vector3),
                    new ParameterSymbol("to", TypeSymbol.Vector3),
                    new ParameterSymbol("amount", TypeSymbol.Float),
                ],
                TypeSymbol.Vector3,
                (call, context) =>
                {
                    object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

                    if (constants is not null)
                    {
                        Vector3F from = (constants[0] as Vector3F?) ?? Vector3F.Zero;
                        Vector3F to = (constants[1] as Vector3F?) ?? Vector3F.Zero;
                        float amount = (constants[2] as float?) ?? 0f;

                        Block vec = context.AddBlock(Blocks.Values.Vector);
                        context.SetBlockValue(vec, 0, from + ((to - from) * amount));

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
                },
                args =>
                {
                    Vector3F from = (Vector3F)args[0].GetValueOrDefault(TypeSymbol.Vector3);
                    Vector3F to = (Vector3F)args[1].GetValueOrDefault(TypeSymbol.Vector3);
                    float amount = (float)args[2].GetValueOrDefault(TypeSymbol.Float);

                    return [
                        from + ((to - from) * amount)
                    ];
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
            """)]
        public static readonly FunctionSymbol AxisAngle
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "axisAngle",
                [
                    new ParameterSymbol("axis", TypeSymbol.Vector3),
                    new ParameterSymbol("angle", TypeSymbol.Float),
                ],
                TypeSymbol.Rotation,
                (call, context) => EmitX1(call, context, Blocks.Math.AxisAngle));

        [FunctionDoc(
            Info = """
            Gets a ray going from (<link type="param">screenX</>, <link type="param">screenY</>).
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
            ])]
        public static readonly FunctionSymbol ScreenToWorld
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "screenToWorld",
                [
                    new ParameterSymbol("screenX", TypeSymbol.Float),
                    new ParameterSymbol("screenY", TypeSymbol.Float),
                    new ParameterSymbol("worldNear", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("worldFar", Modifiers.Out, TypeSymbol.Vector3),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 2, Blocks.Math.ScreenToWorld));

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
            ])]
        public static readonly FunctionSymbol WorldToScreen
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3),
                    new ParameterSymbol("screenX", Modifiers.Out, TypeSymbol.Float),
                    new ParameterSymbol("screenY", Modifiers.Out, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) => EmitXX(call, context, 2, Blocks.Math.WorldToScreen));

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
            ])]
        public static readonly FunctionSymbol WorldToScreen2
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "worldToScreen",
                [
                    new ParameterSymbol("worldPos", TypeSymbol.Vector3),
                ],
                TypeSymbol.Vector3,
                (call, context) =>
                {
                    Block make = context.AddBlock(Blocks.Math.Make_Vector);

                    using (context.ExpressionBlock())
                    {
                        Block wts = context.AddBlock(Blocks.Math.WorldToScreen);

                        using (context.ExpressionBlock())
                        {
                            IEmitStore store = context.EmitExpression(call.Arguments[0]);
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
            ])]
        public static readonly FunctionSymbol LookRotation
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "lookRotation",
                [
                    new ParameterSymbol("direction", TypeSymbol.Vector3),
                    new ParameterSymbol("up", TypeSymbol.Vector3),
                ],
                TypeSymbol.Rotation,
                (call, context) => EmitX1(call, context, Blocks.Math.LookRotation));

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
            ])]
        public static readonly FunctionSymbol LineVsPlane
            = new BuiltinFunctionSymbol(
                MathNamespace,
                "lineVsPlane",
                [
                    new ParameterSymbol("lineFrom", TypeSymbol.Vector3),
                    new ParameterSymbol("lineTo", TypeSymbol.Vector3),
                    new ParameterSymbol("planePoint", TypeSymbol.Vector3),
                    new ParameterSymbol("planeNormal", TypeSymbol.Vector3),
                ],
                TypeSymbol.Vector3,
                (call, context) => EmitX1(call, context, Blocks.Math.LineVsPlane));

        private static readonly Namespace MathNamespace = BuiltinNamespace + "math";
    }
}
