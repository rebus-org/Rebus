using Rebus.Serialization;
using Rebus.Tests.Contracts.Serialization;

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