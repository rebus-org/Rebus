using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Rebus.Activation;
using Rebus.AmazonSQS.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests;
using Rebus.Tests.Integration.ManyMessages;
using Rebus.Threading;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsManyMessagesTransportFactory : IBusFactory
    {
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            var queueName = TestConfig.QueueName(inputQueueAddress);

            PurgeQueue(queueName);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(
                    t =>
                        t.UseAmazonSqs(AmazonSqsTransportFactory.ConnectionInfo.AccessKeyId, AmazonSqsTransportFactory.ConnectionInfo.SecretAccessKey,
                            RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint), queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        static void PurgeQueue(string queueName)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            new AmazonSqsTransport(queueName, AmazonSqsTransportFactory.ConnectionInfo.AccessKeyId, AmazonSqsTransportFactory.ConnectionInfo.SecretAccessKey,
                RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint), consoleLoggerFactory,
                new TplAsyncTaskFactory(consoleLoggerFactory)).Purge();
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}