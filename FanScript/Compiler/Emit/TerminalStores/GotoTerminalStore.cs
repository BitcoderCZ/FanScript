using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;

namespace FanScript.Compiler.Emit.TerminalStores;

internal readonly struct GotoTerminalStore : ITerminalStore
{
	public readonly string LabelName;

	public GotoTerminalStore(string name)
	{
		LabelName = name;
	}

	public ITerminal In => NopTerminal.Instance;

	public ReadOnlySpan<ITerminal> Out => [];
}