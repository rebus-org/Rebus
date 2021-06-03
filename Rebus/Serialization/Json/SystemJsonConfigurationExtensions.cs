#if NETSTANDARD
using System;
using System.Text;
using Newtonsoft.Json;
using Rebus.Config;
using Rebus.Injection;
using System.Text.Json;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Configuration extensions for the .NET System.Text.Json Rebus message serializer
    /// </summary>
    public static class SystemJsonConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use .NET System.Text.Json to serialize messages.
        /// Default <see cref="JsonSerializerOptions" /> settings
        /// Message bodies are UTF8-encoded.
        /// Use this method to to use <see cref="System.Text.Json.JsonSerializer" /> over Newtonsoft.Json
        /// </summary>
        public static void UseSystemJson(this StandardConfigurer<ISerializer> configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            RegisterSerializer(configurer, null, Encoding.UTF8);
        }


        /// <summary>
        /// Configures Rebus to use .NET System.Text.Json to serialize messages, using the specified <see cref="JsonSerializerOptions"/> and 
        /// This allows you to customize almost every aspect of how messages are actually serialized/deserialized.
        /// </summary>
        public static void UseSystemJson(this StandardConfigurer<ISerializer> configurer, JsonSerializerOptions settings, Encoding encoding = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            RegisterSerializer(configurer, settings, encoding ?? Encoding.UTF8);
        }

        static void RegisterSerializer(StandardConfigurer<ISerializer> configurer, JsonSerializerOptions settings, Encoding encoding)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            configurer.OtherService<ISerializer>().Decorate((IResolutionContext c) => new SystemJsonSerializer(c.Get<IMessageTypeNameConvention>(), settings, encoding));
        }
    }
}
#endif