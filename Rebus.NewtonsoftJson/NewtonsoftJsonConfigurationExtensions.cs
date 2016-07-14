using System;
using Newtonsoft.Json;
using Rebus.Config;
using Rebus.Serialization;
using JsonSerializer = Rebus.Serialization.JsonSerializer;

namespace Rebus.NewtonsoftJson
{
    /// <summary>
    /// Configuration extensions for the honest Newtonsoft JSON.NET-based Rebus message serializer
    /// </summary>
    public static class NewtonsoftJsonConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Newtonsoft JSON.NET to serialize messages, using the specified <see cref="JsonSerializerSettings"/>.
        /// This allows you to customize almost every aspect of how messages are actually serialized/deserialized.
        /// </summary>
        public static void UseNewtonsoftJson(this StandardConfigurer<ISerializer> configurer, JsonSerializerSettings settings = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register(c => new JsonSerializer(settings ?? new JsonSerializerSettings()));
        }
    }
}