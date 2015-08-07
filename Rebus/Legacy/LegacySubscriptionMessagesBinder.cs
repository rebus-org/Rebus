using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Rebus.Legacy
{
    /// <summary>
    /// Type binder for JSON.NET that maps old Rebus' SubscriptionMessage to <see cref="LegacySubscriptionMessage"/>
    /// </summary>
    class LegacySubscriptionMessagesBinder : DefaultSerializationBinder
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Binder = new LegacySubscriptionMessagesBinder(),
            TypeNameHandling = TypeNameHandling.All
        };

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
}