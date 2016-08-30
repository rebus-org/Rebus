using Rebus.Serialization;

namespace Rebus.Tests.Serialization
{
    public interface ISerializerFactory
    {
        ISerializer GetSerializer();
    }
}