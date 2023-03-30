using System;
using System.Collections.Concurrent;

namespace Rebus.Tests.Contracts.Extensions;

/// <summary>
/// Extension method that will dispose a <see cref="ConcurrentStack{T}"/> if <see cref="IDisposable"/>s
/// </summary>
public static class DisposableExtensions
{
    /// <summary>
    /// Disposes the <paramref name="disposables"/> by popping &amp; disposing them
    /// </summary>
    public static void Dispose(this ConcurrentStack<IDisposable> disposables)
    {
        if (disposables == null) throw new ArgumentNullException(nameof(disposables));

        while (disposables.TryPop(out var disposable))
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception when disposing {disposable}: {exception}");
            }
        }
    }
}