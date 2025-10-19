using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable EmptyGeneralCatchClause

namespace Rebus.Transport;

class TransactionContext : ICanEagerCommit
{
    // Note: C# generates thread-safe add/remove. They use a compare-and-exchange loop.
    event Func<ITransactionContext, Task> _onCommitted;
    event Func<ITransactionContext, Task> _onRollback;
    event Func<ITransactionContext, Task> _onAck;
    event Func<ITransactionContext, Task> _onNack;
    event Action<ITransactionContext> _onDisposed;

    bool? _mustCommit;
    bool? _mustAck;

    bool _completed;
    bool _disposed;

    public ConcurrentDictionary<string, object> Items { get; } = new();

    public event Action<Exception> OnError;

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

    public void OnRollback(Func<ITransactionContext, Task> rollbackAction)
    {
        if (_completed) ThrowCompletedException();
        _onRollback += rollbackAction;
    }

    public void OnDisposed(Action<ITransactionContext> disposeAction)
    {
        if (_completed) ThrowCompletedException();
        _onDisposed += disposeAction;
    }

    public void SetResult(bool commit, bool ack)
    {
        if (_completed) ThrowCompletedException();
        _mustAck = ack;
        _mustCommit = commit;
    }

    public async Task CommitAsync()
    {
        if (_completed) ThrowCompletedException();
        var onCommitted = Interlocked.Exchange(ref _onCommitted, null);
        await InvokeAsync(onCommitted);
    }

    public async Task Complete()
    {
        if (_mustCommit == null || _mustAck == null)
        {
            throw new InvalidOperationException(
                $"Tried to complete the transaction context, but {nameof(SetResult)} has not been invoked!");
        }

        try
        {
            try
            {
                if (_mustCommit == true)
                {
                    var onCommitted = Interlocked.Exchange(ref _onCommitted, null);
                    await InvokeAsync(onCommitted);
                }
                else
                {
                    var onRollback = Interlocked.Exchange(ref _onRollback, null);
                    await InvokeAsync(onRollback);
                }
            }
            catch
            {
                var onNack = Interlocked.Exchange(ref _onNack, null);

                try
                {
                    await InvokeAsync(onNack);
                }
                catch (Exception exception)
                {
                    OnError?.Invoke(exception);
                }

                throw;
            }

            if (_mustAck == true)
            {
                var onAck = Interlocked.Exchange(ref _onAck, null);
                await InvokeAsync(onAck);
            }
            else
            {
                var onNack = Interlocked.Exchange(ref _onNack, null);
                await InvokeAsync(onNack);
            }
        }
        finally
        {
            _completed = true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // be sure to alway always always!! invoke rollback/NACK if required to do so 
            if (_mustCommit != true)
            {
                try
                {
                    var onRollBack = Interlocked.Exchange(ref _onRollback, null);
                    RebusAsyncHelpers.RunSync(() => InvokeAsync(onRollBack));
                }
                catch (Exception exception)
                {
                    OnError?.Invoke(exception);
                }
            }

            if (_mustAck == false)
            {
                try
                {
                    var onNack = Interlocked.Exchange(ref _onNack, null);
                    RebusAsyncHelpers.RunSync(() => InvokeAsync(onNack));
                }
                catch (Exception exception)
                {
                    OnError?.Invoke(exception);
                }
            }

            try
            {
                _onDisposed?.Invoke(this);
            }
            catch (Exception exception)
            {
                OnError?.Invoke(exception);
            }
        }
        finally
        {
            // wipe all references rooted in the tx context to avoid accidentally capturing anything
            Interlocked.Exchange(ref _onAck, null);
            Interlocked.Exchange(ref _onNack, null);
            Interlocked.Exchange(ref _onCommitted, null);
            Interlocked.Exchange(ref _onRollback, null);
            Interlocked.Exchange(ref _onDisposed, null);
            Interlocked.Exchange(ref OnError, null);
            Items.Clear();

            _disposed = true;
        }
    }

    static void ThrowCompletedException([CallerMemberName] string actionName = null) => throw new InvalidOperationException($"Cannot add {actionName} action on a completed transaction context.");

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