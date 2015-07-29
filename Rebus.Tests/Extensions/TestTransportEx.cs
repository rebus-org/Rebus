using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Extensions
{
    public static class TestTransportEx
    {
        public static async Task<TransportMessage> AwaitReceive(this ITransport transport, double timeoutSeconds = 5)
        {
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (stopwatch.Elapsed < timeout)
            {
                TransportMessage receivedTransportMessage;

                using (var transactionContext = new DefaultTransactionContext())
                {
                    receivedTransportMessage = await transport.Receive(transactionContext);

                    await transactionContext.Complete();
                }

                if (receivedTransportMessage != null) return receivedTransportMessage;
            }

            throw new AssertionException(string.Format("Did not receive transport message from {0} within {1} timeout",
                transport, timeout));
        }
    }
}