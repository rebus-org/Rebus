using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Routing.TypeBased;
using Rebus2.Transport.Msmq;

namespace Tests.Integration
{
    [TestFixture]
    public class TestRetry : FixtureBase
    {
        const string InputQueueName = "test.retries.input";
        BuiltinHandlerActivator _handlerActivator;
        IBus _bus;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Transport(t => t.UseMsmq(InputQueueName, "test.error"))
                .Routing(r => r.SimpleTypeBased().Map<string>(InputQueueName))
                .Start();

            TrackDisposable(_bus);
        }

        [Test]
        public async Task ItWorks()
        {
            _handlerActivator.Handle<string>(async _ =>
            {
                throw new ApplicationException("omgwtf!");
            });

            await _bus.Send("hej");

            await Task.Delay(1000000);
        }
    }
}