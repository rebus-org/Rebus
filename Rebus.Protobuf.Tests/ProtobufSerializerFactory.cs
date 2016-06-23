using ProtoBuf.Meta;
using Rebus.Serialization;
using Rebus.Tests.Serialization;
using Rebus.Tests.Serialization.Default;

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

            _runtimeTypeModel.Add(typeof (RootObject), true)
                .Add(1, "BigObjects");

            _runtimeTypeModel.Add(typeof (BigObject), true)
                .Add(1, "Integer")
                .Add(2, "String");
        }

        public ISerializer GetSerializer()
        {
            return new ProtobufSerializer(_runtimeTypeModel);
        }
    }
}
