﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace InfluxDB.LineProtocol.Collector
{
    class PortableTimer : IDisposable
    {
        readonly object _stateLock = new object();
        PortableTimerState _state = PortableTimerState.NotWaiting;

        readonly Action<CancellationToken> _onTick;
        readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public PortableTimer(Action<CancellationToken> onTick)
        {
            if (onTick == null) throw new ArgumentNullException("onTick");
            _onTick = onTick;
        }

        public async void Start(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException("interval");

            lock (_stateLock)
            {
                if (_state == PortableTimerState.Disposed)
                    throw new ObjectDisposedException("PortableTimer");

                if (_state == PortableTimerState.Waiting)
                    throw new InvalidOperationException("The timer already set");

                if (_cancel.IsCancellationRequested) return;

                _state = PortableTimerState.Waiting;
            }

            try
            {
                await Task.Delay(interval, _cancel.Token);
            }
            finally
            {
                lock (_stateLock)
                    _state = PortableTimerState.NotWaiting;
            }

            if (!_cancel.Token.IsCancellationRequested)
            {
                await Task.Run(() => _onTick(_cancel.Token));
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();

            while (true)
            {
                // Thread.Sleep() would be handy here...

                lock (_stateLock)
                {
                    if (_state == PortableTimerState.Disposed ||
                        _state == PortableTimerState.NotWaiting)
                    {
                        _state = PortableTimerState.Disposed;
                        return;
                    }
                }
            }
        }
    }
}
