using System;

namespace FanScript.Generators;

internal struct Disposable : IDisposable
{
	private Action? _disposeAction;

	public Disposable(Action disposeAction)
	{
		_disposeAction = disposeAction;
	}

	public void Dispose()
	{
		_disposeAction?.Invoke();
		_disposeAction = null;
	}
}
