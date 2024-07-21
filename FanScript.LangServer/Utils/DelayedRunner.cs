using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Utils
{
    internal class DelayedRunner
    {
        private Action action;
        public Action Action
        {
            get => action;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                action = value;
            }
        }

        [MemberNotNullWhen(true, nameof(firstInvoke), nameof(lastInvoke))]
        public bool Scheduled { get; private set; }

        public TimeSpan RunAfter { get; set; }
        public TimeSpan ForceRunAfter { get; set; }

        private DateTime? firstInvoke;
        private DateTime? lastInvoke;

        private object lockObj = new object();

        public DelayedRunner(Action _action, TimeSpan _runAfter, TimeSpan _forceRunAfter)
        {
            ArgumentNullException.ThrowIfNull(_action);
            action = _action;
            RunAfter = _runAfter;
            ForceRunAfter = _forceRunAfter;
        }

        private static Thread? thread;
        private static List<DelayedRunner> scheduled = new();

        private static void startThread()
        {
            if (thread is not null && thread.IsAlive)
                return;

            thread = new Thread(() =>
            {
                int index = 0;

                while (true)
                {
                    Thread.Sleep(1);

                    List<DelayedRunner> toRemove = new();
                    lock (scheduled)
                    {
                        if (scheduled.Count == 0)
                            continue;

                        DateTime now = DateTime.UtcNow;

                        for (int i = 0; i < 10; i++)
                        {
                            if (scheduled[index].checkTimers(now))
                                toRemove.Add(scheduled[index]);

                            index++;

                            if (index >= scheduled.Count)
                                index = 0;
                        }
                    }

                    for (int i = 0; i < toRemove.Count; i++)
                        toRemove[i].invoke();
                }
            });

            thread.Start();
        }

        public void Invoke()
            => Invoke(false);
        public void Invoke(bool forceRun)
        {
            if (forceRun)
            {
                invoke();
                return;
            }

            startThread();

            DateTime now = DateTime.UtcNow;

            lock (lockObj)
            {
                if (!Scheduled)
                {
                    Scheduled = true;
                    firstInvoke = now;
                    lastInvoke = now;

                    lock (scheduled)
                        scheduled.Add(this);
                }
                else
                    lastInvoke = now;
            }
        }

        private void invoke()
        {
            bool wasScheduled;
            lock (lockObj)
                wasScheduled = Scheduled;

            Stop();

            lock (lockObj)
                if (!wasScheduled)
                    return;

            try
            {
                action();
            }
            catch { }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                lock (scheduled)
                    scheduled.Remove(this);

                Scheduled = false;
                firstInvoke = null;
                lastInvoke = null;
            }
        }

        private bool checkTimers(DateTime now)
        {
            lock (lockObj)
                return now - lastInvoke > RunAfter || now - firstInvoke > ForceRunAfter;
        }
    }
}
