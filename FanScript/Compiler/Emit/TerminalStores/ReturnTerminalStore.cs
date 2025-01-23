namespace FanScript.Compiler.Emit.TerminalStores;

internal sealed class ReturnTerminalStore : RollbackTerminalStore
{
	public static new readonly ReturnTerminalStore Instance = new ReturnTerminalStore();
}
