using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.DataBus.FileSystem;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Tests.Integration
{
    [TestFixture]
    [Description(@"Let's just see if we can implement this funny thing:

A little app gets messages sent, and each message has an attachment.

The app automatically cleans up each attachment after having read it.

That's it.")]
    public class TestFileSystemDataBusCleanup : FixtureBase
    {
        InMemNetwork _network;
        string _attachmentsBaseDirectory;
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _network = new InMemNetwork();
            _attachmentsBaseDirectory = NewTempDirectory();

            _activator = Using(new BuiltinHandlerActivator());
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void ItWorks_KeepLastFiveAttachments(int messageCount)
        {
            var counter = Using(new SharedCounter(initialValue: messageCount));

            _activator.Handle<MessageWithAttachment>(async (bus, message) =>
            {
                var dataBus = bus.Advanced.DataBus;
                var attachmentId = message.AttachmentId;

                using (var source = await dataBus.OpenRead(attachmentId))
                using (var reader = new StreamReader(source, Encoding.UTF8))
                {
                    Console.WriteLine($"Got message: {await reader.ReadToEndAsync()}");
                }

                // now clean up
                foreach (var id in dataBus.Query().Skip(5))
                {
                    Console.WriteLine($"*** DELETING ATTACHMENT WITH ID {id} ***");
                    await dataBus.Delete(id);
                }

                counter.Decrement();
            });

            StartBus();

            messageCount.Times(() => SendMessageWithAttachedText("HEJ").Wait());

            counter.WaitForResetEvent(timeoutSeconds: 5);

            var files = Directory.GetFiles(_attachmentsBaseDirectory);

            Assert.That(files.Length, Is.EqualTo(10), $@"Expected 

    {_attachmentsBaseDirectory}

to contain 10 files (5 x .dat + 5 x .meta), but found the following files:

{string.Join(Environment.NewLine, files.Select(filePath => $"    {Path.GetFileName(filePath)}"))}

");
        }

        [Test]
        public async Task ItWorks_DeleteEveryAttachment()
        {
            var counter = Using(new SharedCounter(initialValue: 3));

            _activator.Handle<MessageWithAttachment>(async (bus, message) =>
            {
                var dataBus = bus.Advanced.DataBus;
                var attachmentId = message.AttachmentId;

                using (var source = await dataBus.OpenRead(attachmentId))
                using (var reader = new StreamReader(source, Encoding.UTF8))
                {
                    Console.WriteLine($"Got message: {await reader.ReadToEndAsync()}");
                }

                // now clean up
                await dataBus.Delete(attachmentId);

                counter.Decrement();
            });

            StartBus();

            await SendMessageWithAttachedText("HEJ");
            await SendMessageWithAttachedText("MED");
            await SendMessageWithAttachedText("DIG");

            counter.WaitForResetEvent(timeoutSeconds: 5);

            var files = Directory.GetFiles(_attachmentsBaseDirectory);

            Assert.That(files.Length, Is.EqualTo(0), $@"Expected 

    {_attachmentsBaseDirectory}

to be an empty directory, but found the following files:

{string.Join(Environment.NewLine, files.Select(filePath => $"    {Path.GetFileName(filePath)}"))}

");
        }

        async Task SendMessageWithAttachedText(string text)
        {
            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                var attachment = await _activator.Bus.Advanced.DataBus.CreateAttachment(source);

                await _activator.Bus.SendLocal(new MessageWithAttachment(attachment.Id));
            }
        }

        void StartBus()
        {
            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(_network, "test-queue"))
                .DataBus(d => d.StoreInFileSystem(_attachmentsBaseDirectory))
                .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: 1))
                .Start();
        }

        class MessageWithAttachment
        {
            public string AttachmentId { get; }

            public MessageWithAttachment(string attachmentId)
            {
                AttachmentId = attachmentId;
            }
        }
    }
}