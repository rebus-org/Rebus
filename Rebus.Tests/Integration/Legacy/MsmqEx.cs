using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Rebus.Tests.Integration.Legacy
{
    public static class MsmqEx
    {
        static readonly ExtensionSerializer ExtensionSerialize = new ExtensionSerializer();

        public static void SendLegacyRebusMessage(this MessageQueue queue, string jsonText, Dictionary<string, string> headers)
        {
            var message = new Message
            {
                BodyStream = new MemoryStream(Encoding.UTF7.GetBytes(jsonText)),
                Extension = ExtensionSerialize.Serialize(headers)
            };
            queue.Send(message, MessageQueueTransactionType.Single);
        }

        public static Message GetNextMessage(this MessageQueue queue)
        {
            queue.MessageReadPropertyFilter = new MessagePropertyFilter
            {
                Body = true,
                Extension = true,
            };

            return queue.Receive(TimeSpan.FromSeconds(3));
        }

        public static Dictionary<string, string> DeserializeHeaders(this Message message)
        {
            return ExtensionSerialize.Deserialize(message.Extension);
        }

        class ExtensionSerializer
        {
            static readonly Encoding DefaultLegacyEncoding = Encoding.UTF7;

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                var jsonString = JsonConvert.SerializeObject(headers);

                return DefaultLegacyEncoding.GetBytes(jsonString);
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                var jsonString = DefaultLegacyEncoding.GetString(bytes);

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