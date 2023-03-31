using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestCustomErrorHandler : FixtureBase
{
    BuiltinHandlerActivator _activator;
    CustomErrorHandlerInTheTest _customErrorHandler;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _customErrorHandler = new CustomErrorHandlerInTheTest();

        Configure.With(_activator)
            .Logging(l => l.None())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "custom-error-handler"))
            .Options(o => o.Register<IErrorHandler>(c => _customErrorHandler))
            .Start();
    }

    [Test]
    public async Task ForwardsFailedMessageToCustomErrorHandler()
    {
        _activator.AddHandlerWithBusTemporarilyStopped<string>(async str =>
        {
            Console.WriteLine("Throwing UnauthorizedAccessException");

            throw new UnauthorizedAccessException("don't do that");
        });

        _activator.Bus.SendLocal("hej 2").Wait();

        await _customErrorHandler.FailedMessages.WaitUntil(q => q.Any());

        var transportMessage = _customErrorHandler.FailedMessages.First();

        Assert.That(Encoding.UTF8.GetString(transportMessage.Body), Is.EqualTo(@"""hej 2"""));
    }

    class CustomErrorHandlerInTheTest : IErrorHandler
    {
        public readonly ConcurrentQueue<TransportMessage> FailedMessages = new();

        public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
        {
            FailedMessages.Enqueue(transportMessage);
        }
    }
}