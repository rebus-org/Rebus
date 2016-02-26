using ProtoBuf.Meta;
using Rebus.Serialization;
using Rebus.Tests.Serialization;

namespace Rebus.Protobuf.Tests
{
    public class ProtobufSerializerFactory : ISerializerFactory
    {
        readonly RuntimeTypeModel _runtimeTypeModel;

        public ProtobufSerializerFactory()
        {
            _runtimeTypeModel = TypeModel.Create();

            _runtimeTypeModel.Add(typeof (SomeMessage), true)
                .Add(1, "Text");
        }

        public ISerializer GetSerializer()
        {
            return new ProtobufSerializer(_runtimeTypeModel);
        }
    }
}
