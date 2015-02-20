using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Config;
using Rebus2.Msmq;
using Rebus2.Routing.TypeBased;
using Tests.Extensions;

namespace Tests.Integration
{
    [TestFixture]
    public class TestRequestReply : FixtureBase
    {
        const string InputQueueName = "test.input";

        IBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console())
                .Transport(t => t.UseMsmq(InputQueueName, "test.error"))
                .Routing(r => r.SimpleTypeBased().Map<string>(InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();

            TrackDisposable(_bus);
        }

        [Test]
        public async Task CanSendAndReceive()
        {
            var gotMessage = new ManualResetEvent(false);

            _handlerActivator
                .Handle<string>(async str =>
                {
                    if (str == "hej med dig min ven!")
                    {
                        Console.WriteLine("w00t!");

                        await _bus.Reply("t00t!");
                    }

                    if (str == "t00t!")
                    {
                        gotMessage.Set();
                    }
                });

            await _bus.Send("hej med dig min ven!");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }
}