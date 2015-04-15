using NUnit.Framework;
using Rebus.Serialization;

namespace Rebus.Tests.Serialization
{
    [TestFixture]
    public class JsonSerializerTests : BasicSerializationTests<JsonSerializerFactory> { }

    public class JsonSerializerFactory : ISerializerFactory
    {
        public ISerializer GetSerializer()
        {
            return new JsonSerializer();
        }
    }
}