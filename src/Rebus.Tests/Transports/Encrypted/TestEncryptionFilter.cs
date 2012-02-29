using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Transports.Encrypted;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Transports.Encrypted
{
    [TestFixture]
    public class TestEncryptionFilter : FixtureBase
    {
        const string InitializationVectorBase64 = "OLYKdaDyETlu7NbDMC45dA==";
        const string KeyBase64 = "oA/ZUnFsR9w1qEatOByBSXc4woCuTxmR99tAuQ56Qko=";

        EncryptionFilter filter;
        Sender sender;
        Receiver receiver;

        protected override void DoSetUp()
        {
            sender = new Sender();
            receiver = new Receiver();
            filter = new EncryptionFilter(sender, receiver, InitializationVectorBase64, KeyBase64);
        }

        [Test]
        public void CanEncryptStuff()
        {
            // arrange
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = new Dictionary<string, string> {{"test", "blah!"}},
                                                 Label = "label",
                                                 Body = Encoding.UTF7.GetBytes("Hello world!"),
                                             };

            // act
            filter.Send("test", transportMessageToSend);

            // assert
            var sentMessage = sender.SentMessage;
            sentMessage.Headers.Count.ShouldBe(1);
            sentMessage.Headers["test"].ShouldBe("blah!");
            sentMessage.Label.ShouldBe("label");
            sentMessage.Body.ShouldNotBe(Encoding.UTF7.GetBytes("Hello world!"));

            Console.WriteLine(string.Join(", ", sentMessage.Body.Select(b => b.ToString())));
        }

        [Test]
        public void CanDecryptStuff()
        {
            // arrange
            var encryptedHelloWorldBytes = new byte[]
                                               {
                                                   111, 147, 150, 228, 114, 25, 245, 28, 153, 90, 22, 143, 137, 96, 109,
                                                   236, 57, 161, 207, 128, 130, 72, 246, 159, 144, 29, 130, 179, 87, 32,
                                                   189, 225
                                               };
            
            receiver.SetUpReceive(new ReceivedTransportMessage
                                      {
                                          Id = "id",
                                          Headers = new Dictionary<string, string> {{"test", "blah!"}},
                                          Label = "label",
                                          Body = encryptedHelloWorldBytes,
                                      });
            
            // act
            var receivedTransportMessage = filter.ReceiveMessage();

            // assert
            receivedTransportMessage.Id.ShouldBe("id");
            receivedTransportMessage.Label.ShouldBe("label");
            receivedTransportMessage.Headers.Count.ShouldBe(1);
            receivedTransportMessage.Headers["test"].ShouldBe("blah!");
            Encoding.UTF7.GetString(receivedTransportMessage.Body).ShouldBe("Hello world!");
        }

        [Test]
        public void ItsSymmetric()
        {
            var toSend = new TransportMessageToSend
                             {
                                 Label = Guid.NewGuid().ToString(),
                                 Headers = new Dictionary<string, string>
                                               {
                                                   {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
                                               },
                                 Body = Guid.NewGuid().ToByteArray(),
                             };

            filter.Send("test", toSend);

            var sentMessage = sender.SentMessage;
            var receivedTransportMessage = new ReceivedTransportMessage
                                               {
                                                   Id = Guid.NewGuid().ToString(),
                                                   Label = sentMessage.Label,
                                                   Headers = sentMessage.Headers,
                                                   Body = sentMessage.Body
                                               };

            receiver.SetUpReceive(receivedTransportMessage);

            var receivedMessage = filter.ReceiveMessage();

            receivedMessage.Label.ShouldBe(toSend.Label);
            receivedMessage.Headers.ShouldBe(toSend.Headers);
            receivedMessage.Body.ShouldBe(toSend.Body);
        }

        class Sender : ISendMessages
        {
            public void Send(string destinationQueueName, TransportMessageToSend message)
            {
                SentMessage = message;
            }

            public TransportMessageToSend SentMessage { get; set; }
        }

        class Receiver : IReceiveMessages
        {
            public void SetUpReceive(ReceivedTransportMessage receivedTransportMessage)
            {
                MessageToReceive = receivedTransportMessage;
            }

            public ReceivedTransportMessage MessageToReceive { get; set; }

            public ReceivedTransportMessage ReceiveMessage()
            {
                var receivedTransportMessage = MessageToReceive;
                MessageToReceive = null;
                return receivedTransportMessage;
            }

            public string InputQueue
            {
                get { throw new System.NotImplementedException(); }
            }
        }
    }
}