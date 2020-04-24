using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Exception = System.Exception;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleStringLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Extensions
{
    [TestFixture]
    public class ManualDeadlettering : FixtureBase
    {
        [Test]
        public async Task CanDeadLetterMessageManuallyWithoutAnyNoise()
        {
            var activator = Using(new BuiltinHandlerActivator());

            activator.Handle<string>(async (bus, message) =>
            {
                await bus.Advanced.TransportMessage.Deadletter(errorDetails: "has been manually dead-lettered");
            });

            var fakeErrorHandler = new FakeErrorHandler();

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "manual-deadlettering"))
                .Options(o => o.Register<IErrorHandler>(c => fakeErrorHandler)) //< provide our own implementation here, so we'll know what gets dead-lettered
                .Start();

            await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

            var poisonMessage = await fakeErrorHandler.GetNextPoisonMessage(timeoutSeconds: 2);

            Assert.That(poisonMessage.Item1.Headers, Contains.Key(Headers.ErrorDetails).And.ContainValue("has been manually dead-lettered"));

            Console.WriteLine("Exception passed to error handler:");
            Console.WriteLine(poisonMessage.Item2);
        }

        class FakeErrorHandler : IErrorHandler
        {
            readonly ConcurrentQueue<(TransportMessage, Exception)> _poisonMessages = new ConcurrentQueue<(TransportMessage, Exception)>();

            public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
            {
                _poisonMessages.Enqueue((transportMessage, exception));
            }

            public async Task<(TransportMessage, Exception)> GetNextPoisonMessage(int timeoutSeconds = 5)
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    try
                    {
                        while (true)
                        {
                            if (_poisonMessages.TryDequeue(out var item))
                            {
                                return item;
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationTokenSource.Token);
                        }
                    }
                    catch (Exception)
                    {
                        throw new TimeoutException($"Did not receive poison message within {timeoutSeconds} s timeout");
                    }
                }
            }
        }
    }
}