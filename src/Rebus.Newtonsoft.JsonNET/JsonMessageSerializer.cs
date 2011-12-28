using System;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using System.Linq;

namespace Rebus.Newtonsoft.JsonNET
{
    /// <summary>
    /// Implementation of <see cref="InMemorySubscriptionStorage"/> that uses
    /// the ubiquitous NewtonSoft JSON serializer to serialize and deserialize messages.
    /// </summary>
    public class JsonMessageSerializer : ISerializeMessages
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        static readonly CultureInfo SerializationCulture = CultureInfo.InvariantCulture;

        public TransportMessageToSend Serialize(Message message)
        {
            using (new CultureContext(SerializationCulture))
            {
                var messageAsString = JsonConvert.SerializeObject(message, Formatting.Indented, Settings);

                return new TransportMessageToSend
                           {
                               Data = messageAsString,
                               Headers = message.Headers.ToDictionary(k => k.Key, v => v.Value),
                           };
            }
        }

        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            using (new CultureContext(SerializationCulture))
            {
                var messageAsString = transportMessage.Data;

                return (Message) JsonConvert.DeserializeObject(messageAsString, Settings);
            }
        }

        class CultureContext : IDisposable
        {
            readonly CultureInfo currentCulture;
            readonly CultureInfo currentUiCulture;

            public CultureContext(CultureInfo cultureInfo)
            {
                var thread = Thread.CurrentThread;
                
                currentCulture = thread.CurrentCulture;
                currentUiCulture = thread.CurrentUICulture;

                thread.CurrentCulture = cultureInfo;
                thread.CurrentUICulture = cultureInfo;
            }

            public void Dispose()
            {
                var thread = Thread.CurrentThread;
                
                thread.CurrentCulture = currentCulture;
                thread.CurrentUICulture = currentUiCulture;
            }
        }
    }
}