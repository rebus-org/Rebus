using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Legacy;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestLegacyCompatibility : FixtureBase
    {
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

        [Test]
        public void CanUnderstandOldFormat()
        {
            var newEndpoint = TestConfig.QueueName("newendpoint");
            var oldEndpoint = TestConfig.QueueName("oldendpoint");

            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(newEndpoint));
            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(oldEndpoint));

            var activator = Using(new BuiltinHandlerActivator());

            var gotIt = new ManualResetEvent(false);

            activator.Handle<OldSchoolMessage>(async message =>
            {
                if (message.KeyChar == "g")
                {
                    gotIt.Set();
                }
            });

            Configure.With(activator)
                .Transport(t => t.UseMsmq(newEndpoint))
                .Options(o =>
                {
                    o.EnableLegacyCompatibility();
                    o.LogPipeline(true);
                })
                .Start();

            var correlationId = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();

            using (var queue = new MessageQueue(MsmqUtil.GetFullPath(newEndpoint)))
            {
                var headers = new Dictionary<string, string>
                {
                    {"rebus-return-address", newEndpoint},
                    {"rebus-correlation-id", correlationId},
                    {"rebus-msg-id", messageId},
                    {"rebus-content-type", "text/json"},
                    {"rebus-encoding", "utf-7"}
                };

                var jsonBody = ValidLegacyRebusMessage;

                SendLegacyRebusMessage(queue, jsonBody, headers);
            }

            gotIt.WaitOrDie(TimeSpan.FromSeconds(5));
        }

        static void SendLegacyRebusMessage(MessageQueue queue, string jsonText, Dictionary<string, string> headers)
        {
            var message = new Message
            {
                BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText)),
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

            public Dictionary<string, string> Deserialize(byte[] bytes, string msmqMessageId)
            {
                var jsonString = DefaultEncoding.GetString(bytes);

                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                }
                catch (Exception exception)
                {
                    throw new SerializationException(string.Format("Could not deserialize MSMQ extension for message with physical message ID {0} - expected valid JSON text, got '{1}'",
                        msmqMessageId, jsonString), exception);
                }
            }
        }

    }
}