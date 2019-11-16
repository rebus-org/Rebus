using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Messages.MessageType;
using Rebus.Tests.Contracts;
using Rebus.Topic;

namespace Rebus.Tests.Topic
{
    [TestFixture]
    public class TestDefaultTopicNameConvention
    {
        [Test]
        public void DefaultTopicNameConventionUseGetAssExtension()
        {
            var messageTypeConvetion = new DefaultMessageTypeMapper();
            var convention = new DefaultTopicNameConvention(messageTypeConvetion);

            var expected = messageTypeConvetion.GetMessageType(typeof(SimpleMessage));
            var actual = convention.GetTopic(typeof(SimpleMessage));

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    public class SimpleMessage
    {
        public string Something { get; set; }

    }
}
