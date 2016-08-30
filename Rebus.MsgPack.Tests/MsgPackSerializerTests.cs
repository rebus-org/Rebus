using NUnit.Framework;
using Rebus.Tests.Contracts.Serialization;

namespace Rebus.MsgPack.Tests
{
    [TestFixture]
    public class MsgPackSerializerTests : BasicSerializationTests<MsgPackSerializerFactory>
    {
    }
}
