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

        readonly string _queueName = TestConfig.QueueName("manual_routing");

        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Logging(l => l.Console(minLevel:LogLevel.Warn))
                .Transport(t => t.UseRabbitMq(ConnectionString, _queueName))
                .Start();

            var client = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(client)
                .Logging(l => l.Console(minLevel:LogLevel.Warn))
                .Transport(t => t.UseRabbitMqAsOneWayClient(ConnectionString))
                .Start();
        }

        [Test]
        public async Task ReceivesManuallyRoutedMessage()
        {
            var gotTheMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            Console.WriteLine($"Sending 'hej med dig min ven!' message to '{_queueName}'");

            await _bus.Advanced.Routing.Send(_queueName, "hej med dig min ven!");

            Console.WriteLine("Waiting for message to arrive");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));

            Console.WriteLine("Got it :)");
        }
    }
}