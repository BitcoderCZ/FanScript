using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols
{
    internal static partial class BuiltinFunctions
    {
        private static class Sound
        {
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
                ])]
            public static readonly FunctionSymbol PlaySound
                = new BuiltinFunctionSymbol(
                    SoundNamespace, 
                    "playSound",
                    [
                        new ParameterSymbol("volume", TypeSymbol.Float),
                        new ParameterSymbol("pitch", TypeSymbol.Float),
                        new ParameterSymbol("channel", Modifiers.Out, TypeSymbol.Float),
                        new ParameterSymbol("LOOP", Modifiers.Constant, TypeSymbol.Bool),
                        new ParameterSymbol("SOUND", Modifiers.Constant, TypeSymbol.Float),
                    ], 
                    TypeSymbol.Void, 
                    (call, context) =>
                    {
                        object?[]? values = context.ValidateConstants(call.Arguments.AsMemory(^2..), true);
                        if (values is null)
                        {
                            return NopEmitStore.Instance;
                        }

                        Block playSound = context.AddBlock(Blocks.Sound.PlaySound);

                        context.SetBlockValue(playSound, 0, (byte)(((bool?)values[0] ?? false) ? 1 : 0)); // loop
                        context.SetBlockValue(playSound, 1, (ushort)((float?)values[1] ?? 0f)); // sound

                        using (context.ExpressionBlock())
                        {
                            IEmitStore volume = context.EmitExpression(call.Arguments[0]);
                            IEmitStore pitch = context.EmitExpression(call.Arguments[1]);

                            context.Connect(volume, BasicEmitStore.CIn(playSound, playSound.Type.Terminals["Volume"]));
                            context.Connect(pitch, BasicEmitStore.CIn(playSound, playSound.Type.Terminals["Pitch"]));
                        }

                        IEmitStore varStore;
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
                ])]
            public static readonly FunctionSymbol StopSound
                 = new BuiltinFunctionSymbol(
                     SoundNamespace, 
                     "stopSound",
                     [
                        new ParameterSymbol("channel", TypeSymbol.Float),
                     ], 
                     TypeSymbol.Void, 
                     (call, context) => EmitAX0(call, context, Blocks.Sound.StopSound));

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
                ])]
            public static readonly FunctionSymbol SetVolumePitch
                 = new BuiltinFunctionSymbol(
                     SoundNamespace, 
                     "setVolumePitch",
                     [
                        new ParameterSymbol("channel", TypeSymbol.Float),
                         new ParameterSymbol("volume", TypeSymbol.Float),
                         new ParameterSymbol("pitch", TypeSymbol.Float),
                     ], 
                     TypeSymbol.Void, 
                     (call, context) => EmitAX0(call, context, Blocks.Sound.VolumePitch));

            private static readonly Namespace SoundNamespace = BuiltinNamespace + "sound";
        }
    }
}
