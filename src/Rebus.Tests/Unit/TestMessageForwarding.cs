using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
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
            activateHandlers.Handle<JustSomeMessage>(msg =>
                {
                    Console.WriteLine("EY!!!!!!!!!!---------------------------------");
                    bus.Routing.ForwardCurrentMessage("anotherEndpoint");
                });

            // act
            receiveMessages.Deliver(new Message
                {
                    Headers = new Dictionary<string, string>
                        {
                            {"arbitraryKey", "arbitraryValue"},
                            {"anotherArbitraryKey", "anotherArbitraryValue"}
                        },
                    Messages = new object[] {new JustSomeMessage()}
                });

            Thread.Sleep(0.5.Seconds());

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("anotherEndpoint"),
                                                     Arg<TransportMessageToSend>.Is.Anything));
        }

        class JustSomeMessage {}
    }
}