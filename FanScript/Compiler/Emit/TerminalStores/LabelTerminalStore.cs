using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;

namespace FanScript.Compiler.Emit.TerminalStores;

internal readonly struct LabelTerminalStore : ITerminalStore
{
	public readonly string Name;

	public LabelTerminalStore(string name)
	{
		Name = name;
	}

	public ITerminal In => NopTerminal.Instance;

	public ReadOnlySpan<ITerminal> Out => [];
}
