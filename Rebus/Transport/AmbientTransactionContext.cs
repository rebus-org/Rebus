using System;
using System.Runtime.Remoting.Messaging;

namespace Rebus.Transport
{
    /// <summary>
    /// Provides an "ambient" context for stashing away an instance that implements the current <see cref="ITransactionContext"/>. The
    /// ambient transaction context is automatically preserved even though threads are changed etc.
    /// </summary>
    public static class AmbientTransactionContext
    {
        const string TransactionContextKey = "rebus2-current-transaction-context";

        /// <summary>
        /// Gets the default set function (which is using <see cref="CallContext.LogicalSetData"/> to do its thing)
        /// </summary>
        public static readonly Action<ITransactionContext> DefaultSetter = context => CallContext.LogicalSetData(TransactionContextKey, context);

        /// <summary>
        /// Gets the default set function (which is using <see cref="CallContext.LogicalGetData"/> to do its thing)
        /// </summary>
        public static readonly Func<ITransactionContext> DefaultGetter = () => CallContext.LogicalGetData(TransactionContextKey) as ITransactionContext;

        static Action<ITransactionContext> _setCurrent = DefaultSetter;
        static Func<ITransactionContext> _getCurrent = DefaultGetter;

        /// <summary>
        /// Gets/sets the current transaction context from the call context's logical data slot (which is automatically transferred to continuations when resuming
        /// awaited calls)
        /// </summary>
        public static ITransactionContext Current => _getCurrent();

        /// <summary>
        /// Sets the current transaction context. Please note that in most cases, it is not necessary to set the context using this method
        /// - when using <see cref="DefaultTransactionContext"/> and <see cref="DefaultSyncTransactionContext"/> the ambient transaction context
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
            if (setter == null) throw new ArgumentNullException(nameof(setter));
            if (getter == null) throw new ArgumentNullException(nameof(getter));

            _setCurrent = setter;
            _getCurrent = getter;
        }
    }
}