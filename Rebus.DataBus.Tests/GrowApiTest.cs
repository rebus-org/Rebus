using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Auditing.Sagas;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.DataBus.Tests
{
    [TestFixture]
    public class GrowApiTest : FixtureBase
    {
        InMemNetwork _inMemNetwork;
        IBus _senderBus;
        BuiltinHandlerActivator _receiverActivator;

        protected override void SetUp()
        {
            _inMemNetwork = new InMemNetwork();

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
                    o.EnableDataBus().StoreInMemory();
                })
                .Start();

            return activator;
        }

        [Test]
        public async Task CanSendBigFile()
        {
            var sourceFilePath = GetTempFilePath();
            var destinationFilePath = GetTempFilePath();

            File.WriteAllText(sourceFilePath, "THIS IS A BIG FILE!!");

            var dataSuccessfullyCopied = new ManualResetEvent(false);

            _receiverActivator.Handle<MessageWithAttachment>(async message =>
            {
                var attachment = message.Attachment;

                using (var destination = File.OpenWrite(destinationFilePath))
                {
                    await attachment.OpenRead().CopyToAsync(destination);
                }

                dataSuccessfullyCopied.Set();
            });

            using (var source = File.OpenRead(sourceFilePath))
            {
                var attachment = _senderBus.Advanced.DataBus().CreateAttachment(source)

                var attachment = await DataBusAttachment.FromStream(source, _senderBus);

                await _senderBus.Send(new MessageWithAttachment
                {
                    Attachment = attachment
                });
            }

            dataSuccessfullyCopied.WaitOrDie(TimeSpan.FromSeconds(5), "Data was not successfully copied within 5 second timeout");
        }

        class MessageWithAttachment
        {
            public DataBusAttachment Attachment { get; set; }
        }
    }
}
