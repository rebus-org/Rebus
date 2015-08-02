using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Legacy;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestLegacyCompatibility : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;
        string _newEndpoint;
        string _oldEndpoint;
        const string ValidLegacyRebusMessage = @"{
  ""$type"": ""System.Object[], mscorlib"",
  ""$values"": [
    {
      ""$type"": ""Rebus.Tests.Integration.TestLegacyCompatibility+OldSchoolMessage, Rebus.Tests"",
      ""KeyChar"": ""g""
    }
  ]
}";

        class OldSchoolMessage
        {
            public string KeyChar { get; set; }
        }

        protected override void SetUp()
        {
            _newEndpoint = TestConfig.QueueName("newendpoint");
            _oldEndpoint = TestConfig.QueueName("oldendpoint");

            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(_newEndpoint));
            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(_oldEndpoint));

            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Transport(t => t.UseMsmq(_newEndpoint))
                .Options(o =>
                {
                    o.EnableLegacyCompatibility();
                    //o.LogPipeline(true);
                })
                .Start();
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_newEndpoint);
            MsmqUtil.Delete(_oldEndpoint);
        }

        [Test]
        public void CanSendOldFormat()
        {
            _bus.Route(_oldEndpoint, new OldSchoolMessage {KeyChar = "g"}).Wait();

            using (var queue = new MessageQueue(MsmqUtil.GetFullPath(_oldEndpoint)))
            {
                queue.MessageReadPropertyFilter = new MessagePropertyFilter
                {
                    Body = true,
                    Extension = true,
                };

                var message = queue.Receive(TimeSpan.FromSeconds(3));
                
                using (var streamReader = new StreamReader(message.BodyStream, Encoding.UTF7))
                {
                    var jsonText = streamReader.ReadToEnd();

                    Assert.That(jsonText.ToNormalizedJson(), Is.EqualTo(ValidLegacyRebusMessage.ToNormalizedJson()));
                }

                var headers = new ExtensionSerializer().Deserialize(message.Extension);

                Console.WriteLine(@"Headers:
{0}", string.Join(Environment.NewLine, headers.Select(kvp => string.Format("    {0}: {1}", kvp.Key, kvp.Value))));

                Assert.That(headers["rebus-msg-id"], Is.Not.Empty);
                Assert.That(headers["rebus-content-type"], Is.EqualTo("text/json"));
                Assert.That(headers["rebus-encoding"], Is.EqualTo("utf-7"));
                Assert.That(headers["rebus-return-address"], Is.EqualTo(_newEndpoint));
            }
        }

        [Test]
        public void CanReceiveOldFormat()
        {
            var gotIt = new ManualResetEvent(false);

            _activator.Handle<OldSchoolMessage>(async message =>
            {
                if (message.KeyChar == "g")
                {
                    gotIt.Set();
                }
            });

            var correlationId = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();

            var headers = new Dictionary<string, string>
                {
                    {"rebus-return-address", _newEndpoint},
                    {"rebus-correlation-id", correlationId},
                    {"rebus-msg-id", messageId},
                    {"rebus-content-type", "text/json"},
                    {"rebus-encoding", "utf-7"}
                };

            var jsonBody = ValidLegacyRebusMessage;

            using (var queue = new MessageQueue(MsmqUtil.GetFullPath(_newEndpoint)))
            {
                SendLegacyRebusMessage(queue, jsonBody, headers);
            }

            gotIt.WaitOrDie(TimeSpan.FromSeconds(5));
        }

        static void SendLegacyRebusMessage(MessageQueue queue, string jsonText, Dictionary<string, string> headers)
        {
            var message = new Message
            {
                BodyStream = new MemoryStream(Encoding.UTF7.GetBytes(jsonText)),
                Extension = new ExtensionSerializer().Serialize(headers)
            };
            queue.Send(message, MessageQueueTransactionType.Single);
        }

        class ExtensionSerializer
        {
            static readonly Encoding DefaultEncoding = Encoding.UTF8;

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                var jsonString = JsonConvert.SerializeObject(headers);

                return DefaultEncoding.GetBytes(jsonString);
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                var jsonString = DefaultEncoding.GetString(bytes);

                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                }
                catch (Exception exception)
                {
                    throw new SerializationException("Could not deserialize MSMQ extension", exception);
                }
            }
        }

    }
}