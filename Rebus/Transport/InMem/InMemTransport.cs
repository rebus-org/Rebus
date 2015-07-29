using System;
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
            if (network == null) throw new ArgumentNullException("network");
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");

            _network = network;
            _inputQueueAddress = inputQueueAddress;

            _network.CreateQueue(inputQueueAddress);
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
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (message == null) throw new ArgumentNullException("message");
            if (context == null) throw new ArgumentNullException("context");

            if (!_network.HasQueue(destinationAddress))
            {
                throw new ArgumentException(string.Format("Destination queue address '{0}' does not exist!", destinationAddress));
            }

            context.OnCommitted(async () => _network.Deliver(destinationAddress, message.ToInMemTransportMessage()));
        }

        /// <summary>
        /// Receives the next message from the queue identified by the configured <see cref="Address"/>, returning null if none was available
        /// </summary>
        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var nextMessage = _network.GetNextOrNull(_inputQueueAddress);
            
            if (nextMessage != null)
            {
                context.OnAborted(() =>
                {
                    _network.Deliver(_inputQueueAddress, nextMessage, alwaysQuiet: true);
                });

                return nextMessage.ToTransportMessage();
            }

            await Task.Delay(20);
            
            return null;
        }

        /// <summary>
        /// Initializes the transport by creating its own input queue
        /// </summary>
        public void Initialize()
        {
            CreateQueue(_inputQueueAddress);
        }

        /// <summary>
        /// Gets the input queue
        /// </summary>
        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}