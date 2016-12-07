using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestAsyncHandler : FixtureBase
    {
        static readonly string InputQueueName = TestConfig.GetName("test.async.input");
        readonly IBus _bus;
        readonly BuiltinHandlerActivator _handlerActivator;

        public TestAsyncHandler()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();

            Using(_bus);
        }

        [Fact]
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

            Assert.Equal(4, events.Count);
            Assert.StartsWith("event=1", events[0]);
            Assert.StartsWith("event=2", events[1]);
            Assert.StartsWith("event=3", events[2]);
            Assert.StartsWith("event=4", events[3]);
        }

        static async Task AppendEvent(ICollection<string> events, string eventNumber)
        {
            var text = $"event={eventNumber};thread={Thread.CurrentThread.ManagedThreadId};time={DateTime.UtcNow:mm:ss};context={AmbientTransactionContext.Current}";

            Console.WriteLine(text);

            events.Add(text);

            await Task.Delay(10);
        }
    }
}