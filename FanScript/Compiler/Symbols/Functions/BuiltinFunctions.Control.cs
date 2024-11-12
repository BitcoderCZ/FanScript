using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
    private static class Control
    {
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
            ])]
        public static readonly FunctionSymbol Joystick
            = new BuiltinFunctionSymbol(
                ControlNamespace,
                "joystick",
                [
                    new ParameterSymbol("joyDir", Modifiers.Out, TypeSymbol.Vector3),
                    new ParameterSymbol("JOYSTICK_TYPE", Modifiers.Constant, TypeSymbol.Float),
                ],
                TypeSymbol.Void,
                (call, context) =>
                {
                    object?[]? values = context.ValidateConstants(call.Arguments.AsMemory(Range.StartAt(1)), true);
                    if (values is null)
                    {
                        return NopEmitStore.Instance;
                    }

                    Block joystick = context.AddBlock(Blocks.Control.Joystick);

                    context.SetBlockValue(joystick, 0, (byte)((float?)values[0] ?? 0f)); // unbox, then cast

                    IEmitStore varStore;
                    using (context.StatementBlock())
                    {
                        VariableSymbol variable = ((BoundVariableExpression)call.Arguments[0]).Variable;

                        varStore = context.EmitSetVariable(variable, () => BasicEmitStore.COut(joystick, joystick.Type.Terminals["Joy Dir"]));

                        context.Connect(BasicEmitStore.COut(joystick), varStore);
                    }

                    return new MultiEmitStore(BasicEmitStore.CIn(joystick), varStore is NopEmitStore ? BasicEmitStore.COut(joystick) : varStore);
                });

        private static readonly Namespace ControlNamespace = BuiltinNamespace + "control";
    }
}
