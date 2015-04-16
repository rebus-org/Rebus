using Newtonsoft.Json;
using Rebus.Configuration;

namespace Rebus.NewtonsoftJson
{
    /// <summary>
    /// Configuration extensions for <see cref="NewtonsoftJsonMessageSerializer"/>
    /// </summary>
    public static class NewtonsoftJsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use <see cref="NewtonsoftJsonMessageSerializer"/> to serialize messages. Can pass in a <see cref="JsonSerializerSettings"/> object, 
        /// to configure details around how the JSON serialization should work
        /// </summary>
        public static void UseNewtonsoftJsonSerializer(this RebusSerializationConfigurer configurer, JsonSerializerSettings settings = null)
        {
            var serializer = settings != null
                ? new NewtonsoftJsonMessageSerializer(settings)
                : new NewtonsoftJsonMessageSerializer();

            configurer.Use(serializer);
        }
    }
}