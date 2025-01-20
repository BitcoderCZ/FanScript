using System;
using System.Diagnostics;

namespace FanScript.Generators;
internal struct Disposable : IDisposable
{
	private readonly Action _disposeAction;
	private bool _disposed;

	public Disposable(Action disposeAction)
	{
		Debug.Assert(disposeAction is not null);
		_disposeAction = disposeAction;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_disposeAction();
		}
	}
}
