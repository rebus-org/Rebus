using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests;
using Rebus.Tests.Extensions;

#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    [TestFixture]
    public class TestRabbitMqAndExplicitRouting : FixtureBase
    {
        const string ConnectionString = RabbitMqTransportFactory.ConnectionString;

        IBus _bus;

        protected override void SetUp()
        {
            var client = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(client)
                .Logging(l => l.Console(minLevel:LogLevel.Warn))
                .Transport(t => t.UseRabbitMqAsOneWayClient(ConnectionString))
                .Start();
        }

        [Test]
        public async Task ReceivesManuallyRoutedMessage()
        {
            var queueName = TestConfig.QueueName("manual_routing");
            var gotTheMessage = new ManualResetEvent(false);

            StartServer(queueName).Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            Console.WriteLine($"Sending 'hej med dig min ven!' message to '{queueName}'");

            await _bus.Advanced.Routing.Send(queueName, "hej med dig min ven!");

            Console.WriteLine("Waiting for message to arrive");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));

            Console.WriteLine("Got it :)");
        }

        [Test]
        public async Task AutomaticallyCreatesDestinationQueue()
        {
            var queueName = TestConfig.QueueName("does_not_exist_yet");
            RabbitMqTransportFactory.DeleteQueue(queueName);

            // first we send a message to a queue that does not exist at this time
            Console.WriteLine($"Sending 'hej med dig min ven!' message to '{queueName}'");
            await _bus.Advanced.Routing.Send(queueName, "hej med dig min ven!");

            // then we start a server listening on the queue
            var gotTheMessage = new ManualResetEvent(false);
            StartServer(queueName).Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            Console.WriteLine("Waiting for message to arrive");
            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));
            Console.WriteLine("Got it :)");
        }

        BuiltinHandlerActivator StartServer(string queueName)
        {
            var activator = Using(new BuiltinHandlerActivator());

            Configure.With(activator)
                .Logging(l => l.Console(minLevel: LogLevel.Warn))
                .Transport(t => t.UseRabbitMq(ConnectionString, queueName))
                .Start();

            return activator;
        }
    }
}