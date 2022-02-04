using Rebus.Serialization;

namespace Rebus.Tests.Contracts.Serialization;

public interface ISerializerFactory
{
    ISerializer GetSerializer();
}