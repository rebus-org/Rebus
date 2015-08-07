using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Rebus.Legacy
{
    class LegacySubscriptionMessageSerializer
    {
        public JsonSerializerSettings GetSpecialSettings()
        {
            return _specialSettings;
        }

        readonly JsonSerializerSettings _specialSettings = new JsonSerializerSettings
        {
            Binder = new LegacySubscriptionMessagesBinder(),
            TypeNameHandling = TypeNameHandling.All
        };

        class LegacySubscriptionMessagesBinder : DefaultSerializationBinder
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (serializedType == typeof(LegacySubscriptionMessage))
                {
                    assemblyName = "Rebus";
                    typeName = "Rebus.Messages.SubscriptionMessage";
                    return;
                }
                base.BindToName(serializedType, out assemblyName, out typeName);
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (assemblyName == "Rebus" && typeName == "Rebus.Messages.SubscriptionMessage")
                {
                    return typeof(LegacySubscriptionMessage);
                }
                return base.BindToType(assemblyName, typeName);
            }
        }

        internal class LegacySubscriptionMessage
        {
            public string Type { get; set; }
            public int Action { get; set; }
        }

        public LegacySubscriptionMessage GetAsLegacySubscriptionMessage(string jsonText)
        {
            try
            {
                var messages = JsonConvert.DeserializeObject<object[]>(jsonText, _specialSettings);
                var legacySubscriptionMessage = messages.OfType<LegacySubscriptionMessage>().FirstOrDefault();
                return legacySubscriptionMessage;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
