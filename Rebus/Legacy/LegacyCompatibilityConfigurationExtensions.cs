using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Serialization;
using Rebus.Transport;
using Rebus.Transport.Msmq;
using JsonSerializer = Rebus.Serialization.JsonSerializer;

namespace Rebus.Legacy
{
    /// <summary>
    /// Configuration extensions for enabling legacy compatibility
    /// </summary>
    public static class LegacyCompatibilityConfigurationExtensions
    {
        static readonly JsonSerializerSettings SpecialSettings = new JsonSerializerSettings
        {
            Binder = new LegacySubscriptionMessagesBinder(),
            TypeNameHandling = TypeNameHandling.All
        };

        /// <summary>
        /// Type binder for JSON.NET that maps old Rebus' SubscriptionMessage to <see cref="LegacySubscriptionMessage"/>
        /// </summary>
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

        /// <summary>
        /// Makes Rebus "legacy compatible", i.e. enables wire-level compatibility with older Rebus versions. WHen this is enabled,
        /// all endpoints need to be old Rebus endpoints or new Rebus endpoints with this feature enabled
        /// </summary>
        public static void EnableLegacyCompatibility(this OptionsConfigurer configurer)
        {
            configurer.Register<ISerializer>(c =>
            {
                var specialSettings = SpecialSettings;
                var jsonSerializer = new JsonSerializer(specialSettings, Encoding.UTF7);
                return jsonSerializer;
            });

            configurer.Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();

                // map headers of incoming message from v1 to v2
                pipeline = new PipelineStepConcatenator(pipeline)
                    .OnReceive(new MapLegacyHeadersIncomingStep(), PipelineAbsolutePosition.Front);

                // unpack object[] of transport message
                pipeline = new PipelineStepInjector(pipeline)
                    .OnReceive(new UnpackLegacyMessageIncomingStep(), PipelineRelativePosition.After, typeof (DeserializeIncomingMessageStep));

                // pack into object[]
                pipeline = new PipelineStepInjector(pipeline)
                    .OnSend(new PackLegacyMessageOutgoingStep(), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));

                pipeline = new PipelineStepInjector(pipeline)
                    .OnSend(new MapLegacyHeadersOutgoingStep(), PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep));

                //pipeline = new PipelineStepInjector(pipeline)
                //    .OnReceive(new HandleLegacySubscriptionRequestIncomingStep(c.Get<ISubscriptionStorage>(), c.Get<LegacySubscriptionMessageSerializer>()), PipelineRelativePosition.Before, typeof(MapLegacyHeadersIncomingStep));

                return pipeline;
            });

            configurer.Decorate(c =>
            {
                var transport = c.Get<ITransport>();

                if (transport is MsmqTransport)
                {
                    ((MsmqTransport) transport).UseLegacyHeaderSerialization();
                }

                return transport;
            });
        }
    }
}