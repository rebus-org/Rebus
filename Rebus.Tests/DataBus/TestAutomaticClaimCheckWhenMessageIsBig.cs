using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.ClaimCheck;
using Rebus.DataBus.InMem;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.DataBus
{
    [TestFixture]
    public class TestAutomaticClaimCheckWhenMessageIsBig : FixtureBase
    {
        [Test]
        public async Task ItWorks()
        {
            // installs a transport decorator that throws an exception, if the sent message size exceeds the given threshold
            void FailIfMessageSizeExceeds(OptionsConfigurer optionsConfigurer, int messageSizeLimitBytes) => 
                optionsConfigurer.Decorate<ITransport>(c => new ThrowExceptionsOnBigMessagesTransportDecorator(c.Get<ITransport>(), messageSizeLimitBytes));

            const int limit = 2048;

            var gotTheMessage = new ManualResetEvent(false);

            var activator = new BuiltinHandlerActivator();

            activator.Handle<string>(async message =>
            {
                gotTheMessage.Set();
            });

            Using(activator);

            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "automatic-claim-check"))
                .Options(o =>
                {
                    o.AutomaticallySendBigMessagesAsAttachments(messageSizeThresholdBytes: limit/2);

                    o.LogPipeline(verbose: true);
                })
                .DataBus(d => d.StoreInMemory(new InMemDataStore()))
                .Options(o => FailIfMessageSizeExceeds(o, limit))
                .Start();

            // serialized to JSON encoded as UTF-8, this will be 3 bytes too big (2 bytes for the two ", and one because we add 1 to the size :))
            var bigStringThatWeKnowIsTooBig = new string('*', limit + 1);

            await bus.SendLocal(bigStringThatWeKnowIsTooBig);

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        class ThrowExceptionsOnBigMessagesTransportDecorator : ITransport
        {
            readonly ITransport _transport;
            readonly int _messageSizeLimitBytes;

            public ThrowExceptionsOnBigMessagesTransportDecorator(ITransport transport, int messageSizeLimitBytes)
            {
                _transport = transport;
                _messageSizeLimitBytes = messageSizeLimitBytes;
            }

            public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                var messageSizeBytes = message.Body.Length;

                if (messageSizeBytes > _messageSizeLimitBytes)
                {
                    throw new MessageIsTooBigException($"Message contains {messageSizeBytes} bytes, which is more than the allowed {_messageSizeLimitBytes} bytes");
                }

                return _transport.Send(destinationAddress, message, context);
            }

            public void CreateQueue(string address) => _transport.CreateQueue(address);

            public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken) => _transport.Receive(context, cancellationToken);

            public string Address => _transport.Address;
        }

        class MessageIsTooBigException : Exception
        {
            public MessageIsTooBigException(string message) : base(message)
            {
            }
        }
    }
}