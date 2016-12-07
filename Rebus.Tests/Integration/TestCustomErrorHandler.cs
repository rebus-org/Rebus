using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestCustomErrorHandler : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly CustomErrorHandlerInTheTest _customErrorHandler;

        public TestCustomErrorHandler()
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

        [Fact]
        public async Task ForwardsFailedMessageToCustomErrorHandler()
        {
            _activator.Handle<string>(async str =>
            {
                Console.WriteLine("Throwing AccessViolationException");

                throw new UnauthorizedAccessException("don't do that");
            });

            _activator.Bus.SendLocal("hej 2").Wait();

            await _customErrorHandler.FailedMessages.WaitUntil(q => q.Any());

            var transportMessage = _customErrorHandler.FailedMessages.First();

            Assert.Equal(@"""hej 2""", Encoding.UTF8.GetString(transportMessage.Body));
        }

        class CustomErrorHandlerInTheTest : IErrorHandler
        {
            public readonly ConcurrentQueue<TransportMessage> FailedMessages = new ConcurrentQueue<TransportMessage>();

            public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, string errorDescription)
            {
                FailedMessages.Enqueue(transportMessage);
            }
        }
    }
}