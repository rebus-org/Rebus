using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts.Serialization;

namespace Rebus.Tests.Serialization;

public class JsonSerializerFactory : ISerializerFactory
{
    public ISerializer GetSerializer()
    {
        return new JsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention());
    }
}