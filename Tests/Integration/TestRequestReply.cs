using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Logging;
using Rebus2.Msmq;
using Rebus2.Routing;
using Rebus2.Serialization;

namespace Tests.Integration
{
    [TestFixture]
    public class TestRequestReply : FixtureBase
    {
        const string InputQueueName = "test.input";

        RebusBus _bus;

        protected override void SetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false)
            {
                MinLevel = LogLevel.Debug
            };

            var handlerActivator = new BuiltinHandlerActivator()
                .Handle<string>(async str =>
                {
                    Console.WriteLine("w00t!");
                });

            var router = new SimpleTypeBasedRouter().Map<string>(InputQueueName);
            var transport = new MsmqTransport(InputQueueName);
            var serializer = new JsonSerializer();

            _bus = new RebusBus(handlerActivator, router, transport, serializer);
            
            TrackDisposable(_bus);
            
            _bus.Start();
        }

        [Test]
        public async Task CanSendAndReceive()
        {
            await _bus.Send("hej med dig min ven!");

            await Task.Delay(10000);
        }
    }
}