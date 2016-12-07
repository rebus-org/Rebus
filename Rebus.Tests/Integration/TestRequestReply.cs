using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestRequestReply : FixtureBase
    {
        readonly IBus _bus;
        readonly BuiltinHandlerActivator _handlerActivator;

        public TestRequestReply()
        {
            _handlerActivator = Using(new BuiltinHandlerActivator());

            const string queueName = "request-reply";

            _bus = Configure.With(_handlerActivator)
                .Logging(l => l.Console())
                .Transport(t =>
                {
                    t.UseInMemoryTransport(new InMemNetwork(), queueName);
                })
                .Routing(r => r.TypeBased().Map<string>(queueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Start();
        }

        [Fact]
        public async Task CanSendAndReceive()
        {
            var gotMessage = new ManualResetEvent(false);

            _handlerActivator
                .Handle<string>(async (bus, str) =>
                {
                    if (str == "hej med dig min ven!")
                    {
                        Console.WriteLine("w00t!");

                        await bus.Reply("t00t!");
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