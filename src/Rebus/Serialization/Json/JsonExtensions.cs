using Rebus.Configuration.Configurers;

namespace Rebus.Serialization.Json
{
    public static class JsonExtensions
    {
         public static void UseJsonSerializer(this SerializationConfigurer configurer)
         {
             configurer.Use(new JsonMessageSerializer());
         }
    }
}