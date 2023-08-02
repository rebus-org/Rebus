using System;

namespace Rebus.Tests.Contracts;

/// <summary>
/// Implementation of <see cref="IDisposable"/> that calls an <see cref="Action"/> when disposed
/// </summary>
public class DisposableCallback : IDisposable
{
    readonly Action _action;

    /// <summary>
    /// Creates the disposable callback
    /// </summary>
    public DisposableCallback(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// Disposes the callback, i.e. calls its action
    /// </summary>
    public void Dispose() => _action();
}