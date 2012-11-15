using System.Collections.Generic;
using System.Linq;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer for the various hooks that Rebus provides
    /// </summary>
    public class EventsConfigurer : BaseConfigurer, IRebusEvents
    {
        internal EventsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
            backbone.AddEvents(this);

            MessageMutators = new List<IMutateMessages>();
        }

        /// <summary>
        /// Event that will be raised immediately when the bus is used to send a logical message.
        /// </summary>
        public event MessageSentEventHandler MessageSent;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        public event BeforeMessageEventHandler BeforeMessage;

        /// <summary>
        /// Event that will be raised for each received logical message (i.e. it will only be called
        /// if deserialization completed, and the transport message does in fact contain one or more
        /// logical messages).
        /// </summary>
        public event AfterMessageEventHandler AfterMessage;

        /// <summary>
        /// Event that is raised when an incoming message can be handled by a saga handler, but it
        /// turns out that no saga data instance could be correlated with the message.
        /// </summary>
        public event UncorrelatedMessageEventHandler UncorrelatedMessage;

        /// <summary>
        /// Event that is raised when an incoming transport message has been properly deserialized,
        /// and it is about to be dispatched. The message context will last for the duration of the
        /// message processing and is disposed at the very end.
        /// </summary>
        public event MessageContextEstablishedEventHandler MessageContextEstablished;

        /// <summary>
        /// Event that will be raised immediately after receiving a transport 
        /// message, before any other actions are executed.
        /// </summary>
        public event BeforeTransportMessageEventHandler BeforeTransportMessage;

        /// <summary>
        /// Event that will be raised after a transport message has been handled.
        /// If an error occurs, the caught exception will be passed to the
        /// listeners. If no errors occur, the passed exception will be null.
        /// </summary>
        public event AfterTransportMessageEventHandler AfterTransportMessage;

        /// <summary>
        /// Event that will be raised whenever it is determined that a message
        /// has failed too many times.
        /// </summary>
        public event PoisonMessageEventHandler PoisonMessage;

        /// <summary>
        /// Gets the list of message mutators that should be used to mutate incoming/outgoing messages.
        /// </summary>
        public ICollection<IMutateMessages> MessageMutators { get; private set; }

        internal void TransferToBus(IAdvancedBus advancedBus)
        {
            var rebusEvents = advancedBus.Events;

            if (MessageContextEstablished != null)
            {
                foreach (var listener in MessageContextEstablished.GetInvocationList().Cast<MessageContextEstablishedEventHandler>())
                {
                    rebusEvents.MessageContextEstablished += listener;
                }
            }

            if (MessageSent != null)
            {
                foreach (var listener in MessageSent.GetInvocationList().Cast<MessageSentEventHandler>())
                {
                    rebusEvents.MessageSent += listener;
                }
            }

            if (BeforeMessage != null)
            {
                foreach (var listener in BeforeMessage.GetInvocationList().Cast<BeforeMessageEventHandler>())
                {
                    rebusEvents.BeforeMessage += listener;
                }
            }

            if (AfterMessage != null)
            {
                foreach (var listener in AfterMessage.GetInvocationList().Cast<AfterMessageEventHandler>())
                {
                    rebusEvents.AfterMessage += listener;
                }
            }

            if (BeforeTransportMessage != null)
            {
                foreach (var listener in BeforeTransportMessage.GetInvocationList().Cast<BeforeTransportMessageEventHandler>())
                {
                    rebusEvents.BeforeTransportMessage += listener;
                }
            }

            if (AfterTransportMessage != null)
            {
                foreach (var listener in AfterTransportMessage.GetInvocationList().Cast<AfterTransportMessageEventHandler>())
                {
                    rebusEvents.AfterTransportMessage += listener;
                }
            }

            if (PoisonMessage != null)
            {
                foreach (var listener in PoisonMessage.GetInvocationList().Cast<PoisonMessageEventHandler>())
                {
                    rebusEvents.PoisonMessage += listener;
                }
            }

            if (UncorrelatedMessage != null)
            {
                foreach (var listener in UncorrelatedMessage.GetInvocationList().Cast<UncorrelatedMessageEventHandler>())
                {
                    rebusEvents.UncorrelatedMessage += listener;
                }
            }

            foreach(var messageMutator in MessageMutators)
            {
                rebusEvents.MessageMutators.Add(messageMutator);
            }
        }
    }
}