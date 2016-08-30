using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Rebus.Activation;
using Rebus.AmazonSQS.Config;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsManyMessagesTransportFactory : IBusFactory
    {
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            PurgeQueue(inputQueueAddress);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(
                    t =>
                    {
                        var amazonSqsConfig = new AmazonSQSConfig
                        {
                            RegionEndpoint = RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint)
                        };

                        t.UseAmazonSqs(AmazonSqsTransportFactory.ConnectionInfo.AccessKeyId,
                            AmazonSqsTransportFactory.ConnectionInfo.SecretAccessKey,
                            amazonSqsConfig, inputQueueAddress);
                    })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        public static void PurgeQueue(string queueName)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);

            var amazonSqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint)
            };

            var transport = new AmazonSqsTransport(
                queueName,
                AmazonSqsTransportFactory.ConnectionInfo.AccessKeyId,
                AmazonSqsTransportFactory.ConnectionInfo.SecretAccessKey,
                amazonSqsConfig, consoleLoggerFactory,
                new TplAsyncTaskFactory(consoleLoggerFactory));

            transport.Purge();
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}