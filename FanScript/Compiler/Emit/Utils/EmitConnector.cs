// <copyright file="EmitConnector.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Emit.Utils;

internal class EmitConnector
{
	private readonly Action<IEmitStore, IEmitStore> _connectFunc;

	private IEmitStore? _firstStore;
	private IEmitStore? _lastStore;

	public EmitConnector(Action<IEmitStore, IEmitStore> connectFunc)
	{
		_connectFunc = connectFunc;
	}

	public IEmitStore Store => _firstStore is not null && _lastStore is not null ? new MultiEmitStore(_firstStore, _lastStore) : NopEmitStore.Instance;

	public void Add(IEmitStore store)
	{
		if (store is NopEmitStore)
		{
			return;
		}

		if (_lastStore is not null)
		{
			_connectFunc(_lastStore, store);
		}

		_firstStore ??= store;
		_lastStore = store;
	}
}
