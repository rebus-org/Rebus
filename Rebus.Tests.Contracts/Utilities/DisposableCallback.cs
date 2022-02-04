using System;

namespace Rebus.Tests.Contracts.Utilities;

class DisposableCallback : IDisposable
{
    readonly Action _callback;

    public DisposableCallback(Action callback) => _callback = callback;

    public void Dispose() => _callback();
}