using Rebus.Serialization;
using Rebus.Tests.Serialization;

namespace Rebus.Jil.Tests
{
    public class JilSerializerFactory : ISerializerFactory
    {
        public ISerializer GetSerializer()
        {
            return new JilSerializer();
        }
    }
}