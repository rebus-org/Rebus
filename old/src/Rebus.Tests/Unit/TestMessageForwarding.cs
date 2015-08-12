using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Rhino.Mocks;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestMessageForwarding : RebusBusUnitTestBase
    {
        [Test]
        public void CanForwardLogicalMessageToAnotherEndpoint()
        {
            // arrange
            activateHandlers.Handle<JustSomeMessage>(msg => bus.Routing.ForwardCurrentMessage("anotherEndpoint"));

            // act
            const string arbitrarykey = "arbitraryKey";
            const string anotherArbitraryKey = "anotherArbitraryKey";

            const string arbitraryValue = "arbitraryValue";
            const string anotherArbitraryValue = "anotherArbitraryValue";

            receiveMessages.Deliver(new Message
                {
                    Headers = new Dictionary<string, object>
                        {
                            {arbitrarykey, arbitraryValue},
                            {anotherArbitraryKey, anotherArbitraryValue}
                        },
                    Messages = new object[] {new JustSomeMessage()}
                });

            Thread.Sleep(0.5.Seconds());

            // assert
            sendMessages
                .AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("anotherEndpoint"),
                                             Arg<TransportMessageToSend>
                                                 .Matches(
                                                     m =>
                                                     m.Headers.ContainsKey(arbitrarykey) &&
                                                     m.Headers[arbitrarykey].ToString() == arbitraryValue
                                                     && m.Headers.ContainsKey(anotherArbitraryKey) &&
                                                     m.Headers[anotherArbitraryKey].ToString() == anotherArbitraryValue),
                                             Arg<ITransactionContext>.Is.Anything));
        }


        [Test]
        public void TransfersMessageIdToForwardedMessage()
        {
            var headers = new Dictionary<string, object>
            {
                {Headers.MessageId, "Oh the uniqueness"}
            };

            using (new NoTransaction())
            using (var context = MessageContext.Establish(headers))
            {
                context.SetLogicalMessage(new JustSomeMessage());
                bus.Advanced.Routing.ForwardCurrentMessage("somewhere");
            }

            sendMessages.AssertWasCalled(s =>
                s.Send(Arg<string>.Is.Anything,
					Arg<TransportMessageToSend>.Matches(m => m.Headers[Headers.MessageId].ToString() == "Oh the uniqueness"),
                    Arg<ITransactionContext>.Is.Anything));
        }


        class JustSomeMessage {}
    }


}