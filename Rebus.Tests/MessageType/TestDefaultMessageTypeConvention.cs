using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages.MessageType;
using Rebus.Topic;

namespace Rebus.Tests.MessageType
{
    [TestFixture]
    public class TestDefaultMessageTypeConvention
    {
        private class SimpleMessage
        {
            public string Something { get; set; }
        }

        private class SimpleMessageFromOtherBus
        {
            public string Something { get; set; }
        }

        [Test]
        public void DefaultMessageTypeConventionUseGetAssExtension()
        {
            var typeconvention = new DefaultMessageTypeMapper();

            var expected = typeof(SimpleMessage);
            var name = typeconvention.GetMessageType(typeof(SimpleMessage));
            var actual = typeconvention.GetTypeFromMessage(name);

            Assert.That(actual, Is.EqualTo(expected));
        }

    }
   
}
