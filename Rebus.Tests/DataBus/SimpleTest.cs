using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
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

namespace Rebus.Tests.DataBus;

[TestFixture]
public class SimpleTest : FixtureBase
{
    InMemNetwork _inMemNetwork;
    IBus _senderBus;
    BuiltinHandlerActivator _receiverActivator;
    InMemDataStore _inMemDataStore;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _inMemNetwork = new InMemNetwork();
        _inMemDataStore = new InMemDataStore();

        var (_, senderStarter) = CreateBus("sender");

        _senderBus = senderStarter.Start();

        (_receiverActivator, _starter) = CreateBus("receiver");
    }

    (BuiltinHandlerActivator activator, IBusStarter starter) CreateBus(string queueName)
    {
        var activator = Using(new BuiltinHandlerActivator());

        var starter = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(_inMemNetwork, queueName))
            .Routing(r => r.TypeBased().Map<MessageWithAttachment>("receiver"))
            .DataBus(d =>
            {
                d.UseCompression(DataCompressionMode.Always);
                d.StoreInMemory(_inMemDataStore);
            })
            .Create();

        return (activator, starter);
    }

    [Test]
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

        _starter.Start();

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

        Assert.That(File.ReadAllText(destinationFilePath), Is.EqualTo(originalFileContents));
    }

    class MessageWithAttachment
    {
        public DataBusAttachment Attachment { get; set; }
    }
}