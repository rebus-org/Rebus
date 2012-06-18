using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Shared;
using Rebus.Transports.Encrypted;
using Shouldly;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.Tests.Transports.Encrypted
{
    [TestFixture, Ignore]
    public class TestRijndaelEncryptionTransportDecorator : FixtureBase
    {
        const string InitializationVectorBase64 = "OLYKdaDyETlu7NbDMC45dA==";
        const string KeyBase64 = "oA/ZUnFsR9w1qEatOByBSXc4woCuTxmR99tAuQ56Qko=";

        RijndaelEncryptionTransportDecorator transport;
        Sender sender;
        Receiver receiver;

        protected override void DoSetUp()
        {
            sender = new Sender();
            receiver = new Receiver();
            transport = new RijndaelEncryptionTransportDecorator(sender, receiver, InitializationVectorBase64, KeyBase64);
        }

        [Test]
        public void CanGenerateValidIvAndKey()
        {
            var iv = RijndaelEncryptionTransportDecorator.GenerateIvBase64();
            var key = RijndaelEncryptionTransportDecorator.GenerateKeyBase64();

            var localInstance = new RijndaelEncryptionTransportDecorator(sender, receiver, iv, key);
            
            var toSend = new TransportMessageToSend
            {
                Label = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, string>
                                               {
                                                   {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
                                               },
                Body = Guid.NewGuid().ToByteArray(),
            };

            localInstance.Send("test", toSend);

            var sentMessage = sender.SentMessage;
            var receivedTransportMessage = new ReceivedTransportMessage
            {
                Id = Guid.NewGuid().ToString(),
                Label = sentMessage.Label,
                Headers = sentMessage.Headers,
                Body = sentMessage.Body
            };

            receiver.SetUpReceive(receivedTransportMessage);

            var receivedMessage = localInstance.ReceiveMessage();

            receivedMessage.Label.ShouldBe(toSend.Label);
            var expectedHeaders = toSend.Headers.Clone();
            expectedHeaders[Headers.Encrypted] = null;
            receivedMessage.Headers.ShouldBe(expectedHeaders);
            receivedMessage.Body.ShouldBe(toSend.Body);
        }

        [Test]
        public void AddsHeaderToEncryptedMessage()
        {
            // arrange
            var transportMessageToSend = new TransportMessageToSend { Body = new byte[] { 123, 125 } };

            // act
            transport.Send("somewhere", transportMessageToSend);

            // assert
            sender.SentMessage.Headers.ShouldContainKey(Headers.Encrypted);
        }

        [Test]
        public void DoesntDecryptIfEncrypedHeaderIsNotPresent()
        {
            // arrange
            var messageWithoutEncryptedHeader = new ReceivedTransportMessage { Body = new byte[] { 128 } };
            receiver.MessageToReceive = messageWithoutEncryptedHeader;

            // act
            var receivedTransportMessage = transport.ReceiveMessage();

            // assert
            receivedTransportMessage.Body.ShouldBe(new byte[] { 128 });
        }

        [Test]
        public void CanEncryptStuff()
        {
            // arrange
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = new Dictionary<string, string> { { "test", "blah!" } },
                                                 Label = "label",
                                                 Body = Encoding.UTF7.GetBytes("Hello world!"),
                                             };

            // act
            transport.Send("test", transportMessageToSend);

            // assert
            var sentMessage = sender.SentMessage;
            sentMessage.Headers.Count.ShouldBe(2);
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
                                          Headers = new Dictionary<string, string>
                                                        {
                                                            { "test", "blah!" },
                                                            { Headers.Encrypted, null}
                                                        },
                                          Label = "label",
                                          Body = encryptedHelloWorldBytes,
                                      });

            // act
            var receivedTransportMessage = transport.ReceiveMessage();

            // assert
            receivedTransportMessage.Id.ShouldBe("id");
            receivedTransportMessage.Label.ShouldBe("label");
            receivedTransportMessage.Headers.Count.ShouldBe(2);
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

            transport.Send("test", toSend);

            var sentMessage = sender.SentMessage;
            var receivedTransportMessage = new ReceivedTransportMessage
                                               {
                                                   Id = Guid.NewGuid().ToString(),
                                                   Label = sentMessage.Label,
                                                   Headers = sentMessage.Headers,
                                                   Body = sentMessage.Body
                                               };

            receiver.SetUpReceive(receivedTransportMessage);

            var receivedMessage = transport.ReceiveMessage();

            receivedMessage.Label.ShouldBe(toSend.Label);
            var expectedHeaders = toSend.Headers.Clone();
            expectedHeaders[Headers.Encrypted] = null;
            receivedMessage.Headers.ShouldBe(expectedHeaders);
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
                get { throw new NotImplementedException(); }
            }

            public string InputQueueAddress
            {
                get { throw new NotImplementedException(); }
            }

            public string ErrorQueue
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}