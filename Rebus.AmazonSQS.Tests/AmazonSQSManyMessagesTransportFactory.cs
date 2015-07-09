using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Rebus.Activation;
using Rebus.AmazonSQS.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSQSManyMessagesTransportFactory : IBusFactory
    {
        private readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            var queueName = TestConfig.QueueName(inputQueueAddress);

            PurgeQueue(queueName);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(
                    t =>
                        t.UseAmazonSqs(AmazonSQSTransportFactory.ConnectionInfo.AccessKeyId, AmazonSQSTransportFactory.ConnectionInfo.SecretAccessKey,
                            RegionEndpoint.GetBySystemName(AmazonSQSTransportFactory.ConnectionInfo.RegionEndpoint), queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        private static void PurgeQueue(string queueName)
        {
            new AmazonSqsTransport(queueName, AmazonSQSTransportFactory.ConnectionInfo.AccessKeyId, AmazonSQSTransportFactory.ConnectionInfo.SecretAccessKey,
                RegionEndpoint.GetBySystemName(AmazonSQSTransportFactory.ConnectionInfo.RegionEndpoint)).Purge();

        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}