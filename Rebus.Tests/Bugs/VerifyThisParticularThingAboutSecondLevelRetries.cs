using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
public class VerifyThisParticularThingAboutSecondLevelRetries : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var hasDeferredMessage = Using(new ManualResetEvent(false));
        var activator = Using(new BuiltinHandlerActivator());

        activator.Register((bus, mc) => new Handler(mc, bus, hasDeferredMessage));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Timeouts(t => t.StoreInMemory())
            .Options(o =>
            {
                o.SimpleRetryStrategy(maxDeliveryAttempts: 2,
                    secondLevelRetriesEnabled: true,
                    errorQueueAddress: "poison");

                o.Decorate<IErrorHandler>(c => new MyErrorHandler(c.Get<IErrorHandler>()));
            })
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG");

        hasDeferredMessage.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    class MyErrorHandler : IErrorHandler
    {
        readonly IErrorHandler _errorHandler;

        public MyErrorHandler(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
        {
            return _errorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
    }

    class Handler : IHandleMessages<string>, IHandleMessages<IFailed<object>>
    {
        readonly IBus _bus;
        readonly ManualResetEvent _hasDeferredMessage;
        readonly IMessageContext _messageContext;

        public Handler(IMessageContext messageContext, IBus bus, ManualResetEvent hasDeferredMessage)
        {
            _messageContext = messageContext;
            _bus = bus;
            _hasDeferredMessage = hasDeferredMessage;
        }

        public async Task Handle(string message)
        {
            Console.WriteLine("Handle(string message): {0}", message);
            throw new Exception("Handle(string message)");
        }

        public async Task Handle(IFailed<Object> message)
        {
            Console.WriteLine("Handle(IFailed<Object> message): {0}", message);
            await _bus.Advanced.TransportMessage.Defer(TimeSpan.FromSeconds(2));
            _hasDeferredMessage.Set();
        }
    }
}