using System.Runtime.Serialization.Formatters.Binary;
using Rebus.Configuration.Configurers;
using Rebus.Serialization.Binary;
using Rebus.Serialization.Json;

namespace Rebus.Serialization
{
    public static class SerializationConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to serialize messages using the internal JSON serializer.
        /// </summary>
        public static void UseJsonSerializer(this SerializationConfigurer configurer)
        {
            configurer.Use(new JsonMessageSerializer());
        }

        /// <summary>
        /// Configures Rebus to serialize messages using .NET's <see cref="BinaryFormatter"/>.
        /// </summary>
        public static void UseBinarySerializer(this SerializationConfigurer configurer)
        {
            configurer.Use(new BinaryMessageSerializer());
        }
    }
}