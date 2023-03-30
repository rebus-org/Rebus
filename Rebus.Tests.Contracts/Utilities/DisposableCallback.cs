using System;

namespace Rebus.Tests.Contracts.Utilities;

/// <summary>
/// Disposable that calls an <see cref="Action"/> when disposed
/// </summary>
public class DisposableCallback : IDisposable
{
    readonly Action _callback;

    /// <summary>
    /// Creates the disposable
    /// </summary>
    public DisposableCallback(Action callback) => _callback = callback;

    /// <summary>
    /// Disposes the disposable = calls the callback
    /// </summary>
    public void Dispose() => _callback();
}