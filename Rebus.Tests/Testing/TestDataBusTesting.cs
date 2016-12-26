using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Handlers;
using Rebus.Testing;
using Rebus.Testing.Events;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.Tests.Testing
{
    [TestFixture]
    [Description("How to simulate reading a data bus attachment when unit testing (as in: really isolated) handlers")]
    public class TestDataBusTesting : FixtureBase
    {
        [Test]
        public async Task CreateTest()
        {
            var dataStore = new InMemDataStore();
            var fakeBus = new FakeBus { Advanced = new FakeAdvancedApi(dataBus: new FakeDataBus(dataStore)) };
            var handler = new DataBusAttachmentCreatingHandler(fakeBus);

            await handler.Handle("hej med dig min ven!");

            var sentDataBusAttachments = fakeBus.Events
                .OfType<MessageSent<DataBusAttachment>>()
                .ToList();

            Assert.That(sentDataBusAttachments.Count, Is.EqualTo(1));

            var attachmentId = sentDataBusAttachments.First().CommandMessage.Id;

            var data = dataStore.Load(attachmentId);
            var textData = Encoding.UTF8.GetString(data);

            Assert.That(textData, Is.EqualTo("hej med dig min ven!"));
        }

        class DataBusAttachmentCreatingHandler : IHandleMessages<string>
        {
            readonly IBus _bus;

            public DataBusAttachmentCreatingHandler(IBus bus)
            {
                _bus = bus;
            }
            public async Task Handle(string message)
            {
                using (var source = new MemoryStream(Encoding.UTF8.GetBytes(message)))
                {
                    var dataBusAttachment = await _bus.Advanced.DataBus.CreateAttachment(source);

                    await _bus.Send(dataBusAttachment);
                }
            }
        }

        [Test]
        public async Task ReadTest()
        {
            const string textData = "this is the payload!!";
            var receivedTextData = new List<string>();
            var receivedMetadata = new List<Dictionary<string, string>>();
            var gotMessage = new ManualResetEvent(false);
            var handler = new DataBusAttachmentReadingHandler(receivedTextData, gotMessage, receivedMetadata);

            var dataStore = new InMemDataStore();
            dataStore.Save("this is an attachment id", Encoding.UTF8.GetBytes(textData), new Dictionary<string, string>
            {
                {"custom-meta", "whee!!"}
            });

            using (FakeDataBus.EstablishContext(dataStore))
            {
                await handler.Handle("this is an attachment id");
            }

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1));

            Assert.That(receivedTextData.Count, Is.EqualTo(1));
            Assert.That(receivedTextData.First(), Is.EqualTo(textData));

            Assert.That(receivedMetadata.Count, Is.EqualTo(1));
            Assert.That(receivedMetadata.First()["custom-meta"], Is.EqualTo("whee!!"));
        }

        class DataBusAttachmentReadingHandler : IHandleMessages<string>
        {
            readonly List<string> _receivedTextData;
            readonly ManualResetEvent _gotMessage;
            readonly List<Dictionary<string, string>> _receivedMetadata;

            public DataBusAttachmentReadingHandler(List<string> receivedTextData, ManualResetEvent gotMessage, List<Dictionary<string, string>> receivedMetadata)
            {
                _receivedTextData = receivedTextData;
                _gotMessage = gotMessage;
                _receivedMetadata = receivedMetadata;
            }

            public async Task Handle(string message)
            {
                using (var destination = new MemoryStream())
                {
                    var attachment = new DataBusAttachment(message);

                    using (var source = await attachment.OpenRead())
                    {
                        await source.CopyToAsync(destination);
                    }

                    _receivedTextData.Add(Encoding.UTF8.GetString(destination.ToArray()));

                    var metadata = await attachment.GetMetadata();

                    _receivedMetadata.Add(metadata);
                }

                _gotMessage.Set();
            }
        }
    }
}