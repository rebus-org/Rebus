using NUnit.Framework;
using Rebus.Tests.Serialization;

namespace Rebus.MsgPack.Tests
{
    [TestFixture]
    public class MsgPackSerializerTests : BasicSerializationTests<MsgPackSerializerFactory> { }
}
