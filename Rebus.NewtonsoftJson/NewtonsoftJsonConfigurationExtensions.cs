using Newtonsoft.Json;
using Rebus.Config;
using Rebus.Serialization;
using JsonSerializer = Rebus.Serialization.JsonSerializer;

namespace Rebus.NewtonsoftJson
{
    public static class NewtonsoftJsonConfigurationExtensions
    {
        public static void UseNewtonsoftJson(this StandardConfigurer<ISerializer> configurer, JsonSerializerSettings settings)
        {
            configurer.Register(c => new JsonSerializer(settings));
        }
    }
}