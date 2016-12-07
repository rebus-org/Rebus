using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Compression;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.DataBus
{
    public class SimpleTest : FixtureBase
    {
        readonly InMemNetwork _inMemNetwork;
        readonly IBus _senderBus;
        readonly BuiltinHandlerActivator _receiverActivator;
        readonly InMemDataStore _inMemDataStore;

        public SimpleTest()
        {
            _inMemNetwork = new InMemNetwork();
            _inMemDataStore = new InMemDataStore();

            _senderBus = StartBus("sender").Bus;
            _receiverActivator = StartBus("receiver");
        }

        BuiltinHandlerActivator StartBus(string queueName)
        {
            var activator = Using(new BuiltinHandlerActivator());

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(_inMemNetwork, queueName))
                .Routing(r => r.TypeBased().Map<MessageWithAttachment>("receiver"))
                .Options(o =>
                {
                    o.EnableDataBus()
                        .UseCompression(DataCompressionMode.Always)
                        .StoreInMemory(_inMemDataStore);
                })
                .Start();

            return activator;
        }

        [Fact]
        public async Task CanSendBigFile()
        {
            var sourceFilePath = GetTempFilePath();
            var destinationFilePath = GetTempFilePath();

            const string originalFileContents = "THIS IS A BIG FILE!!";

            File.WriteAllText(sourceFilePath, originalFileContents);

            var dataSuccessfullyCopied = new ManualResetEvent(false);

            // set up handler that writes the contents of the received attachment to a file
            _receiverActivator.Handle<MessageWithAttachment>(async message =>
            {
                var attachment = message.Attachment;

                using (var destination = File.OpenWrite(destinationFilePath))
                using (var stream = await attachment.OpenRead())
                {
                    await stream.CopyToAsync(destination);
                }

                dataSuccessfullyCopied.Set();
            });

            // send a message that sends the contents of a file as an attachment
            using (var source = File.OpenRead(sourceFilePath))
            {
                var optionalMetadata = new Dictionary<string, string>
                {
                    {"username", "ExampleUserName" }
                };
                var attachment = await _senderBus.Advanced.DataBus.CreateAttachment(source, optionalMetadata);

                await _senderBus.Send(new MessageWithAttachment
                {
                    Attachment = attachment
                });
            }

            dataSuccessfullyCopied.WaitOrDie(TimeSpan.FromSeconds(5), "Data was not successfully copied within 5 second timeout");

            Assert.Equal(originalFileContents, File.ReadAllText(destinationFilePath));
        }

        class MessageWithAttachment
        {
            public DataBusAttachment Attachment { get; set; }
        }
    }
}
