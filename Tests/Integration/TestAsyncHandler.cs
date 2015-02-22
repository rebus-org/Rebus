using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Routing.TypeBased;
using Rebus2.Transport;
using Rebus2.Transport.Msmq;
using Tests.Extensions;

namespace Tests.Integration
{
    [TestFixture]
    public class TestAsyncHandler : FixtureBase
    {
        const string InputQueueName = "test.async.input";
        IBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();
            _bus = Configure.With(_handlerActivator)
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Transport(t => t.UseMsmq(InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();

            TrackDisposable(_bus);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(InputQueueName);
        }

        [Test]
        public async Task YeahItWorks()
        {
            var events = new List<string>();
            var finishedHandled = new ManualResetEvent(false);

            _handlerActivator.Handle<string>(async str =>
            {
                AppendEvent(events);
                await Task.Delay(200);
                AppendEvent(events);
                await Task.Delay(200);
                AppendEvent(events);
                await Task.Delay(200);
                AppendEvent(events);
                finishedHandled.Set();
            });

            Console.WriteLine(string.Join(Environment.NewLine, events));

            await _bus.Send("hej med dig!");

            finishedHandled.WaitOrDie(TimeSpan.FromSeconds(10));
        }

        void AppendEvent(List<string> events)
        {
            var text = string.Format("thread={0};time={1};context={2}", 
                Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow.ToString("mm:ss"), AmbientTransactionContext.Current);
            Console.WriteLine(text);
            events.Add(text);
        }
    }
}