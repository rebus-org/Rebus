using Rebus.Serialization;

namespace Rebus.Tests.Serialization
{
    public class JsonSerializerFactory : ISerializerFactory
    {
        public ISerializer GetSerializer()
        {
            return new JsonSerializer();
        }
    }
}