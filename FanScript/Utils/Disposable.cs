﻿// <copyright file="Disposable.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Utils;

internal class Disposable : IDisposable
{
	private Action? _onDispose;

	public Disposable(Action? onDispose)
	{
		_onDispose = onDispose;
	}

	public void Dispose()
	{
		_onDispose?.Invoke();
		_onDispose = null;
	}
}
