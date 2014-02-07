using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Rebus.Transports.Encrypted;
using Rebus.Logging;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestEncryptionAndCompression : FixtureBase, IDetermineMessageOwnership
    {
        const string SenderQueueName = "test.encryptionAndCompression.sender";
        const string ReceiverQueueName = "test.encryptionAndCompression.receiver";
        const string ErrorQueueName = "error";

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            MsmqUtil.Delete(SenderQueueName);
            MsmqUtil.Delete(ReceiverQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void CanSendAndReceiveMessageWithAllPossibleCombinations(bool encrypt, bool compress)
        {
            const string someGreetingWeCanRecognize = "Hello there, my friend!!";
            const string someReplyWeCanRecognize = "Hello back";

            var sender = GetBus(SenderQueueName, encrypt, compress);
            var receiver = GetBus(ReceiverQueueName, encrypt, compress);

            var resetEvent = new ManualResetEvent(false);

            // when the receiver gets the recognizable message, he replied back
            receiver.Handle<string>(str =>
                {
                    if (str == someGreetingWeCanRecognize)
                    {
                        var reply = someReplyWeCanRecognize;

                        var incomingHeaders = MessageContext
                            .GetCurrent()
                            .Headers;

                        if (incomingHeaders.ContainsKey(TellTaleSender.MessageWasCompressedKey))
                        {
                            reply += "|compressed";
                        }

                        if (incomingHeaders.ContainsKey(TellTaleSender.MessageWasEncryptedKey))
                        {
                            reply += "|encrypted";
                        }

                        Console.WriteLine("Got string request: {0} - replying back with {1}", str, reply);

                        receiver.Bus.Reply(reply);
                    }
                });

            // when the sender received the recognizable reply back, he pulls the reset event
            sender.Handle<string>(str =>
                {
                    if (!str.Contains(someReplyWeCanRecognize)) return;

                    if (encrypt && !str.Contains("encrypted"))
                    {
                        Console.WriteLine("Received string did not contain 'encrypted': {0}", str);
                        return;
                    }
                    
                    if (compress && !str.Contains("compressed"))
                    {
                        Console.WriteLine("Received string did not contain 'compressed': {0}", str);
                        return;
                    }

                    Console.WriteLine("Got string reply: {0}", str);

                    resetEvent.Set();
                });

            sender.Bus.Send(someGreetingWeCanRecognize);

            var timeout = 4.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive reply back within {0} timeout", timeout);
        }

        BuiltinContainerAdapter GetBus(string inputQueueName, bool encryption, bool compression)
        {
            var adapter = TrackDisposable(new BuiltinContainerAdapter());

            MsmqUtil.PurgeQueue(inputQueueName);

            Configure.With(adapter)
                     .Logging(l => l.ColoredConsole(LogLevel.Warn))
                     .Transport(t => t.UseMsmq(inputQueueName, ErrorQueueName))
                     .MessageOwnership(o => o.Use(this))
                     .Decorators(d =>
                         {
                             // make this the first decoration step and thus the innermost decorator
                             d.AddDecoration(b => b.SendMessages = new TellTaleSender(b.SendMessages));

                             if (encryption)
                             {
                                 d.EncryptMessageBodies("NLEJVjDYnKfxEUAl2gxflFJfixHwh94iWDltaoayjTM=");
                             }

                             if (compression)
                             {
                                 d.CompressMessageBodies(0);
                             }
                         })
                     .CreateBus()
                     .Start();

            return adapter;
        }

        class TellTaleSender : ISendMessages
        {
            public const string MessageWasCompressedKey = "sent message was compressed";
            public const string MessageWasEncryptedKey = "sent message was encrypted";
            readonly ISendMessages innerSender;

            public TellTaleSender(ISendMessages innerSender)
            {
                this.innerSender = innerSender;
            }

            public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
            {
                var headers = message.Headers;

                if (headers.ContainsKey(Headers.Compression))
                {
                    headers[MessageWasCompressedKey] = true;
                }

                if (headers.ContainsKey(Headers.Encrypted))
                {
                    headers[MessageWasEncryptedKey] = true;
                }

                innerSender.Send(destinationQueueName, message, context);
            }
        }

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(string))
            {
                return ReceiverQueueName;
            }

            throw new ArgumentException(string.Format("Don't know who owns {0}", messageType));
        }
    }
}