using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestRequestReply : FixtureBase
{
    IBus _bus;
    BuiltinHandlerActivator _handlerActivator;

    protected override void SetUp()
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

    [Test]
    public async Task CanSendAndReceive()
    {
        var gotMessage = new ManualResetEvent(false);

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async (bus, str) =>
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

    [Test]
    public async Task SetsReplyToHeader()
    {
        var gotMessage = new ManualResetEvent(false);
        var receivedInReplyToHeaderValue = "not set";

        _handlerActivator
            .AddHandlerWithBusTemporarilyStopped<Request>(async (bus, request) =>
            {
                await bus.Reply(new Reply());
            })
            .AddHandlerWithBusTemporarilyStopped<Reply>(async (bus, context, reply) =>
            {
                receivedInReplyToHeaderValue = context.Headers.GetValueOrNull(Headers.InReplyTo);
                gotMessage.Set();
            });


        const string knownMessageId = "known-id";

        await _bus.SendLocal(new Request(), new Dictionary<string, string> { { Headers.MessageId, knownMessageId } });

        gotMessage.WaitOrDie(TimeSpan.FromSeconds(5));

        Assert.That(receivedInReplyToHeaderValue, Is.EqualTo(knownMessageId));
    }

    class Request { }
    class Reply { }
}