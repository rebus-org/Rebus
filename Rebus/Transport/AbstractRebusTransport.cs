using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Transport
{
    /// <summary>
    /// Abstract transport implementation that implements the necessary logic to queue outgoing messages in memory,
    /// enabling batching and whatnot if desired when sending the messages at commit time
    /// </summary>
    public abstract class AbstractRebusTransport : ITransport
    {
        static readonly Task<int> TaskCompletedResult = Task.FromResult(0);

        /// <summary>
        /// Creates the abstract Rebus transport with the given <paramref name="inputQueueName"/> (or NULL, if it's a one-way client)
        /// </summary>
        protected AbstractRebusTransport(string inputQueueName)
        {
            Address = inputQueueName;
        }

        /// <summary>
        /// Enqueues a message in-mem to be sent when the transaction context is committed
        /// </summary>
        /// <inheritdoc />
        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var outgoingMessages = context.GetOrAdd("outgoing-messages", () =>
            {
                var messages = new ConcurrentQueue<OutgoingMessage>();

                context.OnCommitted(async () => await SendOutgoingMessages(messages, context));

                return messages;
            });

            outgoingMessages.Enqueue(new OutgoingMessage(message, destinationAddress));

            return TaskCompletedResult;
        }

        /// <summary>
        /// Creates a queue with the given name
        /// </summary>
        public abstract void CreateQueue(string address);

        /// <summary>
        /// Receives the next message
        /// </summary>
        public abstract Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Implement this to send all outgoing messages
        /// </summary>
        protected abstract Task SendOutgoingMessages(IEnumerable<OutgoingMessage> outgoingMessages, ITransactionContext context);
        
        /// <summary>
        /// Gets the transport's input queue address
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// Represents one single transport messages for one particular destination
        /// </summary>
        public class OutgoingMessage
        {
            /// <summary>
            /// Gets the transport message
            /// </summary>
            public TransportMessage TransportMessage { get; }

            /// <summary>
            /// Gets the destination address
            /// </summary>
            public string DestinationAddress { get; }

            /// <summary>
            /// Constructs the outgoing message
            /// </summary>
            public OutgoingMessage(TransportMessage transportMessage, string destinationAddress)
            {
                TransportMessage = transportMessage;
                DestinationAddress = destinationAddress;
            }
        }
    }
}