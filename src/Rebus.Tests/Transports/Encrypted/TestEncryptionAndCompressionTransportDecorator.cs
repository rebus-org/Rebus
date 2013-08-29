using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Shared;
using Rebus.Transports.Encrypted;
using Shouldly;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.Tests.Transports.Encrypted
{
    [TestFixture]
    public class TestEncryptionAndCompressionTransportDecorator : FixtureBase
    {
        const string KeyBase64 = "0Y67WrbVDnZurwljr9nI7RuWMiNtctEU3CMZ71NcKuA=";
        const int CompressionThresholdBytes = 2048;

        EncryptionAndCompressionTransportDecorator transport;
        Sender sender;
        Receiver receiver;

        protected override void DoSetUp()
        {
            sender = new Sender();
            receiver = new Receiver();
            transport = new EncryptionAndCompressionTransportDecorator(sender, receiver);

            Console.WriteLine(RijndaelHelper.GenerateNewKey());
        }

        void EnableEncryption()
        {
            transport.EnableEncryption(KeyBase64);
        }

        void EnableCompression()
        {
            transport.EnableCompression(CompressionThresholdBytes);
        }

        [TestCase(100, false)]
        [TestCase(CompressionThresholdBytes, false)]
        [TestCase(CompressionThresholdBytes + 1, true)]
        [TestCase(10 * CompressionThresholdBytes, true)]
        public void CanCompressMessageWhenTheSizeExceedsThreshold(int messageSizeBytes, bool expectSentMessageToBeCompressed)
        {
            // arrange
            EnableCompression();

            // act
            var bodyBytes = Enumerable.Range(0, messageSizeBytes)
                                      .Select(i => (byte) (i%256))
                                      .ToArray();

            transport.Send("wherever",
                           new TransportMessageToSend
                               {
                                   Body = bodyBytes
                               },
                           new NoTransaction());

            // assert
            if (expectSentMessageToBeCompressed)
            {
                sender.SentMessage.Body.Length.ShouldBeLessThan(bodyBytes.Length);
            }
            else
            {
                sender.SentMessage.Body.Length.ShouldBe(bodyBytes.Length);
            }
        }

        [Test]
        public void DoesNotDieWhenReturnedMessageIsNull()
        {
            // arrange
            EnableEncryption();
            receiver.SetUpReceive(null);

            // act
            var receivedTransportMessage = transport.ReceiveMessage(new NoTransaction());

            // assert
            receivedTransportMessage.ShouldBe(null);
        }

        [Test]
        public void CanGenerateValidKey()
        {
            var key = EncryptionAndCompressionTransportDecorator.GenerateKeyBase64();

            var localInstance = new EncryptionAndCompressionTransportDecorator(sender, receiver)
                .EnableEncryption(key);

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
            EnableEncryption();
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
            EnableEncryption();
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
            EnableEncryption();
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
            EnableEncryption();
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

        /// <summary>
        /// 100000 (bytes -> compression -> encryption):
        /// Transport message to send:
        ///     headers: some random header: b270caeb-43ad-4f0c-b154-6f5dfdd41cc9, another random header: 4a7e7ffe-0426-46cc-80ba-831663d9a537
        ///     body size: 1600000
        /// Wire transport message:
        ///     headers: some random header: b270caeb-43ad-4f0c-b154-6f5dfdd41cc9, another random header: 4a7e7ffe-0426-46cc-80ba-831663d9a537, rebus-compression: gzip, rebus-encrypted: , rebus-salt: zN++MFD+mRuRp6KTSldMQg==
        ///     body size: 3168
        /// Received transport message:
        ///     headers: some random header: b270caeb-43ad-4f0c-b154-6f5dfdd41cc9, another random header: 4a7e7ffe-0426-46cc-80ba-831663d9a537
        ///     body size: 1600000
        /// 
        /// 100000 (bytes -> encryption -> compression):
        /// Transport message to send:
        ///    headers: some random header: fd5a4d0a-4723-4866-a9a3-d271bc90c23e, another random header: d17fa3e9-4fdd-4eca-801e-75d926d1a123
        ///    body size: 1600000
        ///
        /// Wire transport message:
        ///    headers: some random header: fd5a4d0a-4723-4866-a9a3-d271bc90c23e, another random header: d17fa3e9-4fdd-4eca-801e-75d926d1a123, rebus-encrypted: , rebus-salt: 6fjLnwkOwAVOO7dOp86mdQ==, rebus-compression: gzip
        ///    body size: 1600524
        ///
        /// Received transport message:
        ///    headers: some random header: fd5a4d0a-4723-4866-a9a3-d271bc90c23e, another random header: d17fa3e9-4fdd-4eca-801e-75d926d1a123
        ///    body size: 1600000
        /// 
        /// CONCLUSION: FIRST, we compress. THEN we encrypt.
        /// </summary>
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(10000)]
        [TestCase(100000)]
        public void ItsSymmetric(int howManyGuidsToSend)
        {
            EnableEncryption();
            EnableCompression();

            var semiRandomBytes = Enumerable
                .Repeat(Guid.NewGuid(), howManyGuidsToSend)
                .SelectMany(guid => guid.ToByteArray())
                .ToArray();

            var someCustomHeaders =
                new Dictionary<string, object>
                    {
                        {"some random header", Guid.NewGuid().ToString()},
                        {"another random header", Guid.NewGuid().ToString()}
                    };

            var messageToSend =
                new TransportMessageToSend
                             {
                                 Label = Guid.NewGuid().ToString(),
                                 Headers = someCustomHeaders,
                                 Body = semiRandomBytes
                             };
            transport.Send("test", messageToSend, new NoTransaction());

            var wireMessage = sender.SentMessage;
            var receivedTransportMessage = wireMessage.ToReceivedTransportMessage();

            receiver.SetUpReceive(receivedTransportMessage);
            var receivedMessage = transport.ReceiveMessage(new NoTransaction());

            Console.WriteLine(@"
Transport message to send:
    headers: {1}
    body size: {0}

Wire transport message:
    headers: {3}
    body size: {2}

Received transport message:
    headers: {5}
    body size: {4}
",
                              messageToSend.Body.Length,
                              FormatHeaders(messageToSend.Headers),

                              wireMessage.Body.Length,
                              FormatHeaders(wireMessage.Headers),

                              receivedMessage.Body.Length,
                              FormatHeaders(receivedMessage.Headers));

            receivedMessage.Label.ShouldBe(messageToSend.Label);
            var expectedHeaders = messageToSend.Headers.Clone();
            receivedMessage.Headers.ShouldBe(expectedHeaders);
            receivedMessage.Body.ShouldBe(messageToSend.Body);
        }

        static string FormatHeaders(IEnumerable<KeyValuePair<string, object>> headers)
        {
            return string.Join(", ", headers.Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value)));
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
        }
    }
}