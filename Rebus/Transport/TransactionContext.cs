using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ForCanBeConvertedToForeach

namespace Rebus.Transport;

class TransactionContext : ITransactionContext
{
    // Note: C# generates thread-safe add/remove. They use a compare-and-exchange loop.
    event Func<ITransactionContext, Task> _onCommitted;
    event Func<ITransactionContext, Task> _onCompleted;
    event Action<ITransactionContext> _onAborted;
    event Action<ITransactionContext> _onDisposed;

    bool _mustAbort;
    bool _completed;
    bool _aborted;
    bool _cleanedUp;
    bool _disposed;

    public ConcurrentDictionary<string, object> Items { get; } = new();

    public void OnCommitted(Func<ITransactionContext, Task> commitAction)
    {
        if (_completed) ThrowCompletedException();

        _onCommitted += commitAction;
    }

    public void OnCompleted(Func<ITransactionContext, Task> completedAction)
    {
        if (_completed) ThrowCompletedException();

        _onCompleted += completedAction;
    }

    public void OnAborted(Action<ITransactionContext> abortedAction)
    {
        if (_completed) ThrowCompletedException();

        _onAborted += abortedAction;
    }

    public void OnDisposed(Action<ITransactionContext> disposedAction)
    {
        if (_completed) ThrowCompletedException();

        _onDisposed += disposedAction;
    }

    public void Abort() => _mustAbort = true;

    public Task Commit() => RaiseCommitted();

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (!_completed)
            {
                RaiseAborted();
            }
        }
        finally
        {
            _disposed = true;

            if (!_cleanedUp)
            {
                try
                {
                    _onDisposed?.Invoke(this);
                }
                finally
                {
                    _cleanedUp = true;
                }
            }
        }
    }

    public async Task Complete()
    {
        if (_mustAbort)
        {
            RaiseAborted();
            return;
        }

        await RaiseCommitted();

        await RaiseCompleted();

        Dispose();
    }

    static void ThrowCompletedException([CallerMemberName] string actionName = null) => throw new InvalidOperationException($"Cannot add {actionName} action on a completed transaction context.");

    void RaiseAborted()
    {
        if (_aborted) return;
        _onAborted?.Invoke(this);
        _aborted = true;
    }

    Task RaiseCommitted()
    {
        // RaiseCommitted() can be called multiple time.
        // So we atomically extract the current list of subscribers and reset the event to null (empty)
        var onCommitted = Interlocked.Exchange(ref _onCommitted, null);
        return InvokeAsync(onCommitted);
    }

    async Task RaiseCompleted()
    {
        await InvokeAsync(_onCompleted);
        _completed = true;
    }

    async Task InvokeAsync(Func<ITransactionContext, Task> actions)
    {
        if (actions == null) return;

        var delegates = actions.GetInvocationList();

        for (var index = 0; index < delegates.Length; index++)
        {
            // they're always of this type, so no need to check the type here
            var asyncTxContextCallback = (Func<ITransactionContext, Task>)delegates[index];

            await asyncTxContextCallback(this);
        }
    }
}