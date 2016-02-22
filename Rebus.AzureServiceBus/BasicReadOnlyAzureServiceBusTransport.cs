using System;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.AzureServiceBus
{
    public class BasicReadOnlyAzureServiceBusTransport : BasicAzureServiceBusTransport
    {
        public BasicReadOnlyAzureServiceBusTransport(string connectionString, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory) : base(connectionString, inputQueueAddress, rebusLoggerFactory, asyncTaskFactory)
        {
        }

        public override void Initialize()
        {
            Log.Info("Initializing Azure Service Bus transport with queue '{0}'", InputQueueAddress);
        }

        public override Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            throw new NotSupportedException("Not able to send. The bus is configured as receiveonly");
        }
    }
}