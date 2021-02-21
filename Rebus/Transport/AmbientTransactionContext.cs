using System;
using System.Threading;

namespace Rebus.Transport
{
    /// <summary>
    /// Provides an "ambient" context for stashing away an instance that implements the current <see cref="ITransactionContext"/>. The
    /// ambient transaction context is automatically preserved even though threads are changed etc.
    /// </summary>
    public static class AmbientTransactionContext
    {
        static readonly AsyncLocal<ITransactionContext> AsyncLocalTxContext = new AsyncLocal<ITransactionContext>();
      
        /// <summary>
        /// Gets the default set function (which is using <see cref="AsyncLocal{T}"/> to do its thing)
        /// </summary>
        public static readonly Action<ITransactionContext> DefaultSetter = context => AsyncLocalTxContext.Value = context;

        /// <summary>
        /// Gets the default set function (which is using <see cref="AsyncLocal{T}"/> to do its thing)
        /// </summary>
        public static readonly Func<ITransactionContext> DefaultGetter = () => AsyncLocalTxContext.Value;

        static Action<ITransactionContext> _setCurrent = DefaultSetter;
        static Func<ITransactionContext> _getCurrent = DefaultGetter;

        /// <summary>
        /// Gets/sets the current transaction context from the call context's logical data slot (which is automatically transferred to continuations when resuming
        /// awaited calls)
        /// </summary>
        public static ITransactionContext Current => _getCurrent();

        /// <summary>
        /// Sets the current transaction context. Please note that in most cases, it is not necessary to set the context using this method
        /// - when using <see cref="RebusTransactionScope"/> and <see cref="RebusTransactionScope"/> the ambient transaction context
        /// is automatically set/unset when the object is created/disposed.
        /// </summary>
        public static void SetCurrent(ITransactionContext transactionContext)
        {
            _setCurrent(transactionContext);
        }

        /// <summary>
        /// Sets the accessor functions used by Rebus to set the current transaction when executing handlers and
        /// getting the context by picking it up when sending messages
        /// </summary>
        public static void SetAccessors(Action<ITransactionContext> setter, Func<ITransactionContext> getter)
        {
            _setCurrent = setter ?? throw new ArgumentNullException(nameof(setter));
            _getCurrent = getter ?? throw new ArgumentNullException(nameof(getter));
        }
    }
}