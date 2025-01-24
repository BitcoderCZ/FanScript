// <copyright file="DelayedRunner.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace FanScript.LangServer.Utils;

internal sealed class DelayedRunner
{
	private static readonly List<DelayedRunner> ScheduledList = [];

	private static Thread? _thread;

	private readonly Lock _lock = new Lock();

	private Action _action;

	private DateTime? _firstInvoke;

	private DateTime? _lastInvoke;

	public DelayedRunner(Action action, TimeSpan runAfter, TimeSpan forceRunAfter)
	{
		ArgumentNullException.ThrowIfNull(action);
		_action = action;
		RunAfter = runAfter;
		ForceRunAfter = forceRunAfter;
	}

	public Action Action
	{
		get => _action;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_action = value;
		}
	}

	[MemberNotNullWhen(true, nameof(_firstInvoke), nameof(_lastInvoke))]
	public bool Scheduled { get; private set; }

	public TimeSpan RunAfter { get; set; }

	public TimeSpan ForceRunAfter { get; set; }

	public void Invoke()
		=> Invoke(false);

	public void Invoke(bool forceRun)
	{
		if (forceRun)
		{
			InvokeInternal();
			return;
		}

		StartThread();

		DateTime now = DateTime.UtcNow;

		lock (_lock)
		{
			if (!Scheduled)
			{
				Scheduled = true;
				_firstInvoke = now;
				_lastInvoke = now;

				lock (ScheduledList)
				{
					ScheduledList.Add(this);
				}
			}
			else
			{
				_lastInvoke = now;
			}
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			lock (ScheduledList)
			{
				ScheduledList.Remove(this);
			}

			Scheduled = false;
			_firstInvoke = null;
			_lastInvoke = null;
		}
	}

	private static void StartThread()
	{
		if (_thread is not null && _thread.IsAlive)
		{
			return;
		}

		_thread = new Thread(() =>
		{
			int index = 0;

			while (true)
			{
				Thread.Sleep(1);

				List<DelayedRunner> toRemove = [];
				lock (ScheduledList)
				{
					if (ScheduledList.Count == 0)
					{
						continue;
					}

					DateTime now = DateTime.UtcNow;

					for (int i = 0; i < 10; i++)
					{
						if (ScheduledList[index].CheckTimers(now))
						{
							toRemove.Add(ScheduledList[index]);
						}

						index++;

						if (index >= ScheduledList.Count)
						{
							index = 0;
						}
					}
				}

				for (int i = 0; i < toRemove.Count; i++)
				{
					toRemove[i].InvokeInternal();
				}
			}
		});

		_thread.Start();
	}

	private void InvokeInternal()
	{
		bool wasScheduled;
		lock (_lock)
		{
			wasScheduled = Scheduled;
		}

		Stop();

		lock (_lock)
		{
			if (!wasScheduled)
			{
				return;
			}
		}

		try
		{
			_action();
		}
		catch
		{
		}
	}

	private bool CheckTimers(DateTime now)
	{
		lock (_lock)
		{
			return now - _lastInvoke > RunAfter || now - _firstInvoke > ForceRunAfter;
		}
	}
}
