using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Logging;
using Rebus2.Msmq;
using Rebus2.Pipeline;
using Rebus2.Routing;
using Rebus2.Serialization;
using Tests.Extensions;

namespace Tests.Integration
{
    [TestFixture]
    public class TestRequestReply : FixtureBase
    {
        const string InputQueueName = "test.input";

        RebusBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false)
            {
                MinLevel = LogLevel.Debug
            };

            _handlerActivator = new BuiltinHandlerActivator();

            var router = new SimpleTypeBasedRouter().Map<string>(InputQueueName);
            var transport = new MsmqTransport(InputQueueName);
            var serializer = new JsonSerializer();
            var pipelineManager = new DefaultPipelineManager()
                .Receive(async (context, next) =>
                {
                    Console.WriteLine("First");
                    await next();
                }, "first")
                .Receive(async (context, next) =>
                {
                    Console.WriteLine("Second");
                    await next();
                }, "second")
                .Receive(async (context, next) =>
                {
                    Console.WriteLine("Third");
                    await next();
                }, "third");

            _bus = new RebusBus(_handlerActivator, router, transport, serializer, pipelineManager);
            
            TrackDisposable(_bus);
            
            _bus.Start();
        }

        [Test]
        public async Task CanSendAndReceive()
        {
            var gotMessage = new ManualResetEvent(false);
            
            _handlerActivator
                .Handle<string>(async str =>
                {
                    Console.WriteLine("w00t!");
                    gotMessage.Set();
                });

            await _bus.Send("hej med dig min ven!");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }
}