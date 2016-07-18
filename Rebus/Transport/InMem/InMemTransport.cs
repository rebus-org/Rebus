using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
#pragma warning disable 1998

namespace Rebus.Transport.InMem
{
    /// <summary>
    /// In-mem implementation of <see cref="ITransport"/> that uses one particular <see cref="InMemNetwork"/> to deliver messages. Can
    /// be used for in-process messaging and unit testing
    /// </summary>
    public class InMemTransport : ITransport, IInitializable
    {
        readonly InMemNetwork _network;
        readonly string _inputQueueAddress;

        /// <summary>
        /// Creates the transport, using the specified <see cref="InMemNetwork"/> to deliver/receive messages. This transport will have
        /// <paramref name="inputQueueAddress"/> as its input queue address, and thus will attempt to receive messages from the queue with that
        /// name out of the given <paramref name="network"/>
        /// </summary>
        public InMemTransport(InMemNetwork network, string inputQueueAddress)
        {
            if (network == null) throw new ArgumentNullException(nameof(network), "You need to provide a network that this in-mem transport should use for communication");

            _network = network;
            _inputQueueAddress = inputQueueAddress;
        }

        /// <summary>
        /// Creates a queue with the given address
        /// </summary>
        public void CreateQueue(string address)
        {
            _network.CreateQueue(address);
        }

        /// <summary>
        /// Delivers the given message to the queue identitied by the given <paramref name="destinationAddress"/>
        /// </summary>
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!_network.HasQueue(destinationAddress))
            {
                throw new ArgumentException($"Destination queue address '{destinationAddress}' does not exist!");
            }

            context.OnCommitted(async () => _network.Deliver(destinationAddress, message.ToInMemTransportMessage()));
        }

        /// <summary>
        /// Receives the next message from the queue identified by the configured <see cref="Address"/>, returning null if none was available
        /// </summary>
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (_inputQueueAddress == null) throw new InvalidOperationException("This in-mem transport is initialized without an input queue, hence it is not possible to receive anything!");

            var nextMessage = _network.GetNextOrNull(_inputQueueAddress);
            
            if (nextMessage != null)
            {
                context.OnAborted(() =>
                {
                    _network.Deliver(_inputQueueAddress, nextMessage, alwaysQuiet: true);
                });

                return nextMessage.ToTransportMessage();
            }

            await Task.Delay(5, cancellationToken);
            
            return null;
        }

        /// <summary>
        /// Initializes the transport by creating its own input queue
        /// </summary>
        public void Initialize()
        {
            if (_inputQueueAddress == null) return;

            CreateQueue(_inputQueueAddress);
        }

        /// <summary>
        /// Gets the input queue
        /// </summary>
        public string Address => _inputQueueAddress;
    }
}