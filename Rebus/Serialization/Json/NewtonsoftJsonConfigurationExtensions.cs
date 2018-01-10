using System;
using System.Text;
using Newtonsoft.Json;
using Rebus.Config;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Configuration extensions for the honest Newtonsoft JSON.NET-based Rebus message serializer
    /// </summary>
    public static class NewtonsoftJsonConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Newtonsoft JSON.NET to serialize messages, using <see cref="JsonSerializerSettings"/> that includes ALL
        /// type information in every object, thus allowing for preserving all type information when roundtripping message types.
        /// Message bodies are UTF8-encoded.
        /// This is the default message serialization, so there is actually no need to call this method.
        /// </summary>
        public static void UseNewtonsoftJson(this StandardConfigurer<ISerializer> configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            var settings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

            RegisterSerializer(configurer, settings, Encoding.UTF8);
        }

        /// <summary>
        /// Configures Rebus to use Newtonsoft JSON.NET to serialize messages, using appropriate <see cref="JsonSerializerSettings"/> 
        /// depending on the given <paramref name="mode"/>. Message bodies are UTF8-encoded.
        /// Passing <see cref="JsonInteroperabilityMode.FullTypeInformation"/> as the value for <paramref name="mode"/> is equivalent
        /// to calling <see cref="UseNewtonsoftJson(StandardConfigurer{ISerializer})"/> which in turn
        /// is equivalent to not explicitly configuring the serializer because it's the default serialization :)
        /// </summary>
        public static void UseNewtonsoftJson(this StandardConfigurer<ISerializer> configurer, JsonInteroperabilityMode mode)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            TypeNameHandling GetTypeNameHandling()
            {
                switch (mode)
                {
                    case JsonInteroperabilityMode.FullTypeInformation:
                        return TypeNameHandling.All;
                    case JsonInteroperabilityMode.PureJson:
                        return TypeNameHandling.None;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Unknown {typeof(JsonInteroperabilityMode).Name} value");
                }
            }

            var settings = new JsonSerializerSettings {TypeNameHandling = GetTypeNameHandling()};

            RegisterSerializer(configurer, settings, Encoding.UTF8);
        }

        /// <summary>
        /// Configures Rebus to use Newtonsoft JSON.NET to serialize messages, using the specified <see cref="JsonSerializerSettings"/> and 
        /// This allows you to customize almost every aspect of how messages are actually serialized/deserialized.
        /// </summary>
        public static void UseNewtonsoftJson(this StandardConfigurer<ISerializer> configurer, JsonSerializerSettings settings, Encoding encoding = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            RegisterSerializer(configurer, settings, encoding);
        }

        static void RegisterSerializer(StandardConfigurer<ISerializer> configurer, JsonSerializerSettings settings, Encoding encoding)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register(c => new JsonSerializer(settings, encoding ?? Encoding.UTF8));
        }
    }
}