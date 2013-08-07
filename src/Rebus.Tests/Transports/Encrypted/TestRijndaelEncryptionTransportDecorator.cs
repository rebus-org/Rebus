using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Shared;
using Rebus.Transports.Encrypted;
using Rebus.Transports.Msmq;
using Shouldly;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.Tests.Transports.Encrypted
{
    [TestFixture]
    public class TestRijndaelEncryptionTransportDecorator : FixtureBase
    {
        const string KeyBase64 = "0Y67WrbVDnZurwljr9nI7RuWMiNtctEU3CMZ71NcKuA=";

        RijndaelEncryptionTransportDecorator transport;
        Sender sender;
        Receiver receiver;

        protected override void DoSetUp()
        {
            sender = new Sender();
            receiver = new Receiver();
            transport = new RijndaelEncryptionTransportDecorator(sender, receiver, KeyBase64);

            Console.WriteLine(RijndaelHelper.GenerateNewKey());
        }

        [Test]
        public void DoesNotDieWhenReturnedMessageIsNull()
        {
            // arrange
            receiver.SetUpReceive(null);

            // act
            var receivedTransportMessage = transport.ReceiveMessage(new NoTransaction());

            // assert
            receivedTransportMessage.ShouldBe(null);

        }

        [Test]
        public void CanGenerateValidKey()
        {
            var key = RijndaelEncryptionTransportDecorator.GenerateKeyBase64();

            var localInstance = new RijndaelEncryptionTransportDecorator(sender, receiver, key);

            var toSend = new TransportMessageToSend
            {
                Label = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, object>
                                               {
                                                   {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
                                               },
                Body = Guid.NewGuid().ToByteArray(),
            };

            localInstance.Send("test", toSend, new NoTransaction());

            var receivedTransportMessage = sender.SentMessage.ToReceivedTransportMessage();

            receiver.SetUpReceive(receivedTransportMessage);

            var receivedMessage = localInstance.ReceiveMessage(new NoTransaction());

            receivedMessage.Label.ShouldBe(toSend.Label);
            var expectedHeaders = toSend.Headers.Clone();
            receivedMessage.Headers.ShouldBe(expectedHeaders);
            receivedMessage.Body.ShouldBe(toSend.Body);
        }

        [Test]
        public void AddsHeaderToEncryptedMessage()
        {
            // arrange
            var transportMessageToSend = new TransportMessageToSend { Body = new byte[] { 123, 125 } };

            // act
            transport.Send("somewhere", transportMessageToSend, new NoTransaction());

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
            var receivedTransportMessage = transport.ReceiveMessage(new NoTransaction());

            // assert
            receivedTransportMessage.Body.ShouldBe(new byte[] { 128 });
        }

        [Test]
        public void CanEncryptStuff()
        {
            // arrange
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = new Dictionary<string, object> { { "test", "blah!" } },
                                                 Label = "label",
                                                 Body = Encoding.UTF7.GetBytes("Hello world!"),
                                             };

            // act
            transport.Send("test", transportMessageToSend, new NoTransaction());

            // assert
            var sentMessage = sender.SentMessage;
            sentMessage.Headers.Count.ShouldBe(3);
            sentMessage.Headers["test"].ShouldBe("blah!");
            sentMessage.Label.ShouldBe("label");
            sentMessage.Body.ShouldNotBe(Encoding.UTF7.GetBytes("Hello world!"));

            sentMessage.Headers.ShouldContainKey(Headers.Encrypted);
            sentMessage.Headers.ShouldContainKey(Headers.EncryptionSalt);

            Console.WriteLine("iv: " + sentMessage.Headers[Headers.EncryptionSalt]);
            Console.WriteLine(string.Join(", ", sentMessage.Body.Select(b => b.ToString())));
        }

        [Test]
        public void CanDecryptStuff()
        {
            // arrange
            var encryptedHelloWorldBytes = new byte[]
                {
                    52, 37, 104, 93, 201, 121, 244, 71, 165, 73, 194, 144, 35, 150, 157, 139, 16, 142, 170, 196, 248,
                    208, 185, 230, 222, 115, 52, 141, 247, 33, 253, 200
                };

            receiver.SetUpReceive(new ReceivedTransportMessage
                                      {
                                          Id = "id",
                                          Headers = new Dictionary<string, object>
                                                        {
                                                            { "test", "blah!" },
                                                            { Headers.Encrypted, null},
                                                            {Headers.EncryptionSalt, "IvMyFtbRGH1u8SVpT3iHCg=="}
                                                        },
                                          Label = "label",
                                          Body = encryptedHelloWorldBytes,
                                      });

            // act
            var receivedTransportMessage = transport.ReceiveMessage(new NoTransaction());

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
                                 Headers = new Dictionary<string, object>
                                               {
                                                   {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
                                               },
                                 Body = Guid.NewGuid().ToByteArray(),
                             };

            transport.Send("test", toSend, new NoTransaction());

            var receivedTransportMessage = sender.SentMessage.ToReceivedTransportMessage();

            receiver.SetUpReceive(receivedTransportMessage);

            var receivedMessage = transport.ReceiveMessage(new NoTransaction());

            receivedMessage.Label.ShouldBe(toSend.Label);
            var expectedHeaders = toSend.Headers.Clone();
            receivedMessage.Headers.ShouldBe(expectedHeaders);
            receivedMessage.Body.ShouldBe(toSend.Body);
        }

        class Sender : ISendMessages
        {
            public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
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

            public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
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