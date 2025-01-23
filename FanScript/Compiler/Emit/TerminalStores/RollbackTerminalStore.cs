using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;

namespace FanScript.Compiler.Emit.TerminalStores;

/// <summary>
/// Used by goto rollback, neccesary because special block blocks (play sensor, late update) execute after even if they execute the body, so the after would get executed twice
/// </summary>
internal class RollbackTerminalStore : ITerminalStore
{
	public static readonly RollbackTerminalStore Instance = new RollbackTerminalStore();

	protected RollbackTerminalStore()
	{
	}

	public ITerminal In => NopTerminal.Instance;

	public ReadOnlySpan<ITerminal> Out => [];
}
