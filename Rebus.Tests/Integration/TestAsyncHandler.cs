using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestAsyncHandler : FixtureBase
    {
        static readonly string InputQueueName = TestConfig.QueueName("test.async.input");
        IBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();
            _bus = Configure.With(_handlerActivator)
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task YeahItWorks()
        {
            var events = new List<string>();
            var finishedHandled = new ManualResetEvent(false);

            _handlerActivator.Handle<string>(async str =>
            {
                await AppendEvent(events, "1");
                await AppendEvent(events, "2");
                await AppendEvent(events, "3");
                await AppendEvent(events, "4");
                finishedHandled.Set();
            });

            Console.WriteLine(string.Join(Environment.NewLine, events));

            await _bus.Send("hej med dig!");

            finishedHandled.WaitOrDie(TimeSpan.FromSeconds(10));

            Assert.That(events.Count, Is.EqualTo(4));
            Assert.That(events[0], Is.StringStarting("event=1"));
            Assert.That(events[1], Is.StringStarting("event=2"));
            Assert.That(events[2], Is.StringStarting("event=3"));
            Assert.That(events[3], Is.StringStarting("event=4"));
        }

        async Task AppendEvent(List<string> events, string eventNumber)
        {
            var text = string.Format("event={0};thread={1};time={2};context={3}", 
                eventNumber,
                Thread.CurrentThread.ManagedThreadId, 
                DateTime.UtcNow.ToString("mm:ss"), 
                AmbientTransactionContext.Current);

            Console.WriteLine(text);

            events.Add(text);

            await Task.Delay(10);
        }
    }
}