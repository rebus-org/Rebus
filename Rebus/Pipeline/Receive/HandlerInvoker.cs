using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Sagas;
using Rebus.Transport;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Wrapper of the handler that is ready to invoke
    /// </summary>
    public abstract class HandlerInvoker
    {
        static readonly ConcurrentDictionary<string, bool> CanBeInitiatedByCache = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Gets whether a message of the given type is allowed to cause a new saga data instance to be created
        /// </summary>
        public bool CanBeInitiatedBy(Type messageType)
        {
            // checks if IAmInitiatedBy<TMessage> is implemented by the saga
            return CanBeInitiatedByCache
                .GetOrAdd($"{Handler.GetType().FullName}::{messageType.FullName}", _ =>
                {
                    var implementedInterfaces = Saga.GetType().GetInterfaces();

                    var handlerTypesToLookFor = new[] { messageType }.Concat(messageType.GetBaseTypes())
                        .Select(baseType => typeof(IAmInitiatedBy<>).MakeGenericType(baseType));

                    return implementedInterfaces.Intersect(handlerTypesToLookFor).Any();
                });
        }

        /// <summary>
        /// Key under which the handler invoker will stash itself in the <see cref="ITransactionContext.Items"/>
        /// during the invocation of the wrapped handler
        /// </summary>
        public const string CurrentHandlerInvokerItemsKey = "current-handler-invoker";

        /// <summary>
        /// Method to call in order to invoke this particular handler
        /// </summary>
        public abstract Task Invoke();

        /// <summary>
        /// Gets whether this invoker's handler is a saga
        /// </summary>
        public abstract bool HasSaga { get; }

        /// <summary>
        /// Gets this invoker's handler as a saga (throws if it's not a saga)
        /// </summary>
        public abstract Saga Saga { get; }

        /// <summary>
        /// Adds to the invoker a piece of saga data that has been determined to be relevant for the invocation
        /// </summary>
        public abstract void SetSagaData(ISagaData sagaData);

        /// <summary>
        /// Gets from the invoker the piece of saga data that has been determined to be relevant for the invocation, returning null if no such saga data has been set
        /// </summary>
        public abstract ISagaData GetSagaData();

        /// <summary>
        /// Marks this handler as one to skip, i.e. calling this method will make the invoker ignore the call to <see cref="Invoke"/>
        /// </summary>
        public abstract void SkipInvocation();

        /// <summary>
        /// Gets the contained handler object (which is probably an implementation of <see cref="IHandleMessages"/>, but you should
        /// not depend on it!)
        /// </summary>
        public abstract object Handler { get; }
    }

    /// <summary>
    /// Derivation of the <see cref="HandlerInvoker"/> that has the message type
    /// </summary>
    public class HandlerInvoker<TMessage> : HandlerInvoker
    {
        readonly string _messageId;
        readonly Func<Task> _action;
        readonly object _handler;
        readonly ITransactionContext _transactionContext;
        ISagaData _sagaData;
        bool _invokeHandler = true;

        /// <summary>
        /// Constructs the invoker
        /// </summary>
        public HandlerInvoker(string messageId, Func<Task> action, object handler, ITransactionContext transactionContext)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));
            _messageId = messageId;
            _action = action;
            _handler = handler;
            _transactionContext = transactionContext;
        }

        /// <summary>
        /// Gets the contained handler object
        /// </summary>
        public override object Handler => _handler;

        /// <summary>
        /// Gets whther the contained handler object has a saga
        /// </summary>
        public override bool HasSaga => _handler is Saga;

        /// <summary>
        /// If <see cref="HasSaga"/> returned true, the <see cref="Handler"/> can be retrieved as a <see cref="Saga"/> here
        /// </summary>
        public override Saga Saga
        {
            get
            {
                if (!HasSaga)
                {
                    throw new InvalidOperationException($"Attempted to get {_handler} as saga, it's not a saga!");
                }

                return (Saga)_handler;
            }
        }

        /// <summary>
        /// Invokes the handler within this handler invoker
        /// </summary>
        public override async Task Invoke()
        {
            if (!_invokeHandler) return;

            try
            {
                _transactionContext.Items[CurrentHandlerInvokerItemsKey] = this;

                await _action();
            }
            finally
            {
                object temp;
                _transactionContext.Items.TryRemove(CurrentHandlerInvokerItemsKey, out temp);
            }
        }

        const string SagaDataPropertyName = nameof(Saga<ConcreteSagaData>.Data); //< for refactoring tools to better see it

        class ConcreteSagaData : SagaData { } //< in order to be able to declare the const above

        /// <summary>
        /// Sets a saga instance on the handler
        /// </summary>
        public override void SetSagaData(ISagaData sagaData)
        {
            if (!HasSaga)
            {
                throw new InvalidOperationException($"Attempted to set {sagaData} as saga data on handler {_handler}, but the handler is not a saga!");
            }

            var dataProperty = _handler.GetType().GetProperty(SagaDataPropertyName);

            if (dataProperty == null)
            {
                throw new ApplicationException($"Could not find the '{SagaDataPropertyName}' property on {_handler}...");
            }

            dataProperty.SetValue(_handler, sagaData);

            _sagaData = sagaData;
        }

        /// <summary>
        /// Gets the saga data (if any) that was previously set with <see cref="SetSagaData"/>. Returns null
        /// if none has been set
        /// </summary>
        public override ISagaData GetSagaData()
        {
            return _sagaData;
        }

        /// <summary>
        /// Marks this handler invoker to skip its invocation, causing it to do nothin when <see cref="Invoke"/> is called
        /// </summary>
        public override void SkipInvocation()
        {
            _invokeHandler = false;
        }
    }
}