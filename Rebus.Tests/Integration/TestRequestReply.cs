using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Extensions;
using Rebus.Tests.Transport.Msmq;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestRequestReply : FixtureBase
    {
        static readonly string InputQueueName = MsmqHelper.QueueName("test.input");

        IBus _bus;
        BuiltinHandlerActivator _handlerActivator;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console())
                .Transport(t => t.UseMsmq(InputQueueName))
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();

            TrackDisposable(_bus);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(InputQueueName);
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
                        Console.WriteLine("got t++t!!!");

                        gotMessage.Set();
                    }
                });

            await _bus.Send("hej med dig min ven!");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(30));
        }
    }
}