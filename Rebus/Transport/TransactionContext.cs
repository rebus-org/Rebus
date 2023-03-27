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
    event Func<ITransactionContext, Task> _onAck;
    event Func<ITransactionContext, Task> _onNack;
    event Action<ITransactionContext> _onAborted;
    event Action<ITransactionContext> _onDisposed;

    bool _skipCommit;
    bool _mustAbort;
    bool _completed;
    bool _aborted;
    bool _cleanedUp;
    bool _disposed;

    public ConcurrentDictionary<string, object> Items { get; } = new();

    public void OnCommit(Func<ITransactionContext, Task> commitAction)
    {
        if (_completed) ThrowCompletedException();

        _onCommitted += commitAction;
    }

    public void OnAck(Func<ITransactionContext, Task> ackAction)
    {
        if (_completed) ThrowCompletedException();

        _onAck += ackAction;
    }

    public void OnNack(Func<ITransactionContext, Task> nackAction)
    {
        if (_completed) ThrowCompletedException();

        _onNack += nackAction;
    }

    public void OnRollback(Action<ITransactionContext> rollbackAction)
    {
        if (_completed) ThrowCompletedException();

        _onAborted += rollbackAction;
    }

    public void OnDisposed(Action<ITransactionContext> disposeAction)
    {
        if (_completed) ThrowCompletedException();

        _onDisposed += disposeAction;
    }

    public void Abort() => _mustAbort = true;

    public Task Commit() => RaiseCommitted();

    public void SkipCommit() => _skipCommit = true;

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

    public async Task Complete(bool ack = true)
    {
        // if we must abort, just do that
        if (_mustAbort)
        {
            RaiseAborted();
        }
        else
        {
            await RaiseCommitted();
        }

        await RaiseCompleted(ack);
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

    async Task RaiseCompleted(bool ack)
    {
        await InvokeAsync(ack ? _onAck : _onNack);

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