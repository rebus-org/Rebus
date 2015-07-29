using NUnit.Framework;
using Rebus.Messages;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestMessage
    {
        [TestCase("", "(empty string)")]
        [TestCase("This is just a very long long long long long long long long long long long long long long long long long long string", "This is just a very (...)")]
        [TestCase(@"This is a text
that spans
multiple lines", "This is a text(...)")]
        [TestCase(@"

This is a text
that is prefixed
with multiple blank lines
that spans
multiple lines", "This is a text(...)")]
        [TestCase("/.,\"@ must be invalid", "must be invalid")]
        public void CanGenerateSensibleLabelEvenWhenSendingRawStringsOfArbitraryLengthAndFormatting(string crazyMessageThatMustBeHandled, string expectedLabel)
        {
            // arrange
            var message = new Message
                {
                    Messages = new object[] {crazyMessageThatMustBeHandled}
                };

            // act
            var label = message.GetLabel();

            // assert
            label.ShouldBe(expectedLabel);
        }
    }
}