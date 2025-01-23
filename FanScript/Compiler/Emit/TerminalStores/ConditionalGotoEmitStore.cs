using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;

namespace FanScript.Compiler.Emit.TerminalStores;

internal readonly struct ConditionalGotoTerminalStore : ITerminalStore
{
	public readonly Block OnCondition;
	public readonly TerminalDef OnConditionTerminal;

	private readonly Block _out;
	private readonly TerminalDef _outTerminal;

	public ConditionalGotoTerminalStore(Block @in, TerminalDef inTerminal, Block onCondition, TerminalDef onConditionTerminal, Block @out, TerminalDef outTerminal)
	{
		In = new BlockTerminal(@in, inTerminal);

		_out = @out;
		_outTerminal = outTerminal;

		OnCondition = onCondition;
		OnConditionTerminal = onConditionTerminal;
	}

	public ITerminal In { get; }

	public ReadOnlySpan<ITerminal> Out => new ITerminal[] { new BlockTerminal(_out, _outTerminal) };
}
