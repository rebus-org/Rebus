using Rebus.Serialization;
using Rebus.Tests.Serialization;

namespace Rebus.MsgPack.Tests
{
    public class MsgPackSerializerFactory : ISerializerFactory
    {
        public ISerializer GetSerializer()
        {
            return new MsgPackSerializer();
        }
    }
}