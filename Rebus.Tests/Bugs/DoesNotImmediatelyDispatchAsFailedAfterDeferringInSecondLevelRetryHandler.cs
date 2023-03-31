using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[Description(@"

I reckon this requires an explanation :)

Consider this scenario: You fail while processing a message, which 
then ends up being dispatched as an IFailed<YourMessage>.

You handle IFailed<YourMessage> by deferring it to the future.

When the message returns, somehow the message is still tracked by the 
error tracker - therefore, it will immediately be dispatched as an 
IFailed<YourMessage> again, which is probably not what you had hoped 
for.

It could happen to you too!

(or if I fix this, it can't)

")]
[TestFixture]
public class DoesNotImmediatelyDispatchAsFailedAfterDeferringInSecondLevelRetryHandler : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _busStarter = Configure.With(_activator)
            .Logging(l => l.None())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "check that it works"))
            .Timeouts(t => t.StoreInMemory())
            .Options(o =>
            {
                o.SetRetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1);
            })
            .Create();
    }

    [Test]
    public void DoesNotReceiveSameFailedMessageOverAndOver()
    {
        var events = new ConcurrentQueue<string>();
        var whatToDo = new WhatToDo();

        var messageHandledGood = new ManualResetEvent(false);

        _activator.Register((bus, context) => new YourMessageHandler(bus, whatToDo, messageHandledGood, text =>
        {
            events.Enqueue(text);
            Console.WriteLine(text);
        }));

        _busStarter.Start();

        _activator.Bus.SendLocal(new YourMessage()).Wait();

        messageHandledGood.WaitOrDie(TimeSpan.FromSeconds(6));
    }

    class WhatToDo
    {
        bool _shouldFail = true;

        public bool ShouldFail
        {
            get { return _shouldFail; }
            set
            {
                _shouldFail = value;
                Console.WriteLine($"ShouldFail: {_shouldFail}");
            }
        }
    }

    class YourMessage { }

    class YourMessageHandler : IHandleMessages<YourMessage>, IHandleMessages<IFailed<YourMessage>>
    {
        readonly IBus _bus;
        readonly Action<string> _callback;
        readonly WhatToDo _whatToDo;
        readonly ManualResetEvent _messageHandledGood;

        public YourMessageHandler(IBus bus, WhatToDo whatToDo, ManualResetEvent messageHandledGood, Action<string> callback)
        {
            _bus = bus;
            _callback = callback;
            _whatToDo = whatToDo;
            _messageHandledGood = messageHandledGood;
        }

        public async Task Handle(YourMessage message)
        {
            if (_whatToDo.ShouldFail)
            {
                _callback("Handle YourMessage and fail");
                throw new ArithmeticException("pretend that it didn't work");
            }

            _callback("Handle YourMessage");
            _messageHandledGood.Set();
        }

        public async Task Handle(IFailed<YourMessage> message)
        {
            _callback("Handle IFailed<YourMessage> and defer");

            _whatToDo.ShouldFail = false;

            // this would defer the message with a new ID, so we would not recognize the message when it returned
            //await _bus.Defer(TimeSpan.FromSeconds(1), message.Message);

            // to check that we clear the state as we should, we must defer the actual transport message
            await _bus.Advanced.TransportMessage.Defer(TimeSpan.FromSeconds(1));
        }
    }
}