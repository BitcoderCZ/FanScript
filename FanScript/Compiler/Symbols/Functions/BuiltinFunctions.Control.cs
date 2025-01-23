// <copyright file="BuiltinFunctions.Control.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
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
						return NopTerminalStore.Instance;
					}

					Block joystick = context.AddBlock(StockBlocks.Control.Joystick);

					context.SetSetting(joystick, 0, (byte)((float?)values[0] ?? 0f)); // unbox, then cast

					ITerminalStore varStore;
					using (context.StatementBlock())
					{
						VariableSymbol variable = ((BoundVariableExpression)call.Arguments[0]).Variable;

						varStore = context.EmitSetVariable(variable, () => TerminalStore.COut(joystick, joystick.Type["Joy Dir"]));

						context.Connect(TerminalStore.COut(joystick), varStore);
					}

					return new MultiTerminalStore(TerminalStore.CIn(joystick), varStore is NopTerminalStore ? TerminalStore.COut(joystick) : varStore);
				});

		private static readonly Namespace ControlNamespace = BuiltinNamespace + "control";
	}
}
