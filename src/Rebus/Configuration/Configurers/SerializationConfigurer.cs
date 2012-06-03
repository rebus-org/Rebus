using Rebus.Serialization.Json;

namespace Rebus.Configuration.Configurers
{
    public class SerializationConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public SerializationConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : ISerializeMessages
        {
            containerAdapter.RegisterInstance(instance, typeof(ISerializeMessages));
        }
    }

    public static class SerializationConfigurationExtensions
    {
        public static void UseJsonSerializer(this SerializationConfigurer configurer)
        {
            configurer.Use(new JsonMessageSerializer());
        }
    }
}