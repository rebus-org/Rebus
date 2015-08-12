using System;
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Represents the fact that we're currently handling a message that was correctly correlated with
    /// a saga instance - thus it makes sense to say that we're in a saga context
    /// </summary>
    public class SagaContext : IDisposable
    {
        /// <summary>
        /// The message context itemss key under which this saga context will register itself
        /// </summary>
        public const string SagaContextItemKey = "saga_context";

        readonly IMessageContext messageContext;

        /// <summary>
        /// Gets the ID of the saga data whose context we're currently in
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Constructs the saga context, registering the context under the key
        /// <see cref="SagaContextItemKey"/> in the current message context
        /// (if one is available)
        /// </summary>
        public SagaContext(Guid id)
        {
            Id = id;

            if (MessageContext.HasCurrent)
            {
                messageContext = MessageContext.GetCurrent();
                messageContext.Items[SagaContextItemKey] = this;
            }
        }

        /// <summary>
        /// Makes sure that the saga context is removed from the items dictionary of the current <see cref="MessageContext"/>
        /// </summary>
        public void Dispose()
        {
            if (messageContext != null)
            {
                messageContext.Items.Remove(SagaContextItemKey);
            }
        }
    }
}