using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages.MessageType;
using Rebus.Topic;

namespace Rebus.Tests.MessageType
{
    [TestFixture]
    public class TestMappingMessageTypeConvention
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
        public void MappingMessageTypeConventionUseGetAssExtension()
        {
            var typeconvention = new MessageTypeMapper();
            typeconvention.Map<SimpleMessage>("simpleMessage");

            var typeconventionotherbus = new MessageTypeMapper();
            typeconventionotherbus.Map<SimpleMessageFromOtherBus>("simpleMessage");

            var expected = typeof(SimpleMessageFromOtherBus);
            var name = typeconvention.GetMessageType(typeof(SimpleMessage));
            var actual = typeconventionotherbus.GetTypeFromMessage(name);

            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void MappingMessageTypeConventionThrowExceptionIfNotUnique()
        {
            void CheckFunction()
            {
                var typeconvention = new MessageTypeMapper();
                typeconvention.Map<SimpleMessage>("simpleMessage");
                typeconvention.Map<SimpleMessageFromOtherBus>("simpleMessage");
            }

            Assert.Throws(typeof(RebusConfigurationException), CheckFunction);
        }

    }
}
