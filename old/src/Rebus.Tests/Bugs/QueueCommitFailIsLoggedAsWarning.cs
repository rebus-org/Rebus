using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("Verifies that an exception on queue commit is logged as a warning")]
    public class QueueCommitFailIsLoggedAsWarning : FixtureBase
    {
        BuiltinContainerAdapter adapter;
        List<string> logStatements;
        ArtificialTransport transport;

        protected override void DoSetUp()
        {
            adapter = TrackDisposable(new BuiltinContainerAdapter());
            logStatements = new List<string>();

            transport = new ArtificialTransport();

            Configure.With(adapter)
                .Logging(l => l.Use(new ListLoggerFactory(logStatements)))
                .Transport(t =>
                {
                    t.UseSender(transport);
                    t.UseReceiver(transport);
                    t.UseErrorTracker(new ErrorTracker("error"));
                })
                .CreateBus()
                .Start(1);
        }

        [Test]
        public void VerifyOutputWhenLoggingAnError()
        {
            var list = new List<string>();
            var factory = new ListLoggerFactory(list);
            var logger = factory.GetCurrentClassLogger();
            
            logger.Error(new OmgWtfException("omgwtf!!!"), "Unhandled system exception");

            Console.WriteLine(@"Logger got the following line(s):

{0}

", string.Join(Environment.NewLine, list));

            var loggedLine = list.Single();
            loggedLine.ShouldContain("ERROR: Unhandled system exception");
            loggedLine.ShouldContain(typeof(OmgWtfException).Name);
        }

        [Test]
        public void JustWaitForAWhile()
        {
            adapter.Handle<string>(str =>
            {
                // noop message handler
            });
            transport.Deliver("hello!!!!");

            Thread.Sleep(3.Seconds());

            lock (logStatements)
            {
                Console.WriteLine(string.Join(Environment.NewLine, logStatements));

                var logStatementsWithErrors = logStatements.Where(l => l.Contains("ERROR"));
                logStatementsWithErrors.Count().ShouldBe(0);

                var logStatementsWithWarnings = logStatements.Where(l => l.Contains("WARN"));
                logStatementsWithWarnings.Count().ShouldBeGreaterThan(0);
            }
        }

        class ArtificialTransport : IDuplexTransport
        {
            readonly ConcurrentQueue<ReceivedTransportMessage> subliminalMessages = new ConcurrentQueue<ReceivedTransportMessage>();
            readonly JsonMessageSerializer serializer = new JsonMessageSerializer();
            readonly List<Tuple<string, TransportMessageToSend>> sentMessages = new List<Tuple<string, TransportMessageToSend>>();

            public ArtificialTransport()
            {
                InputQueue = "ArtificialTransport.InputQueue";
            }

            public void Deliver(string text)
            {
                var message =
                    new Message
                    {
                        Headers = new Dictionary<string, object>(),
                        Messages = new object[] { text }
                    };

                var transportMessage = serializer.Serialize(message);

                subliminalMessages.Enqueue(transportMessage.ToReceivedTransportMessage());
            }

            public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
            {
                sentMessages.Add(Tuple.Create(destinationQueueName, message));
            }

            public List<TransportMessageToSend> SentMessages
            {
                get { return sentMessages.Select(s => s.Item2).ToList(); }
            }

            public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
            {
                ReceivedTransportMessage receivedMessage;
                if (subliminalMessages.TryDequeue(out receivedMessage))
                {
                    // simulate real transactional queue behavior
                    if (context.IsTransactional)
                    {
                        context.DoCommit += () =>
                        {
                            throw new OmgWtfException("!!!!");
                        };
                        context.DoRollback += () => subliminalMessages.Enqueue(receivedMessage);
                    }
                    return receivedMessage;
                }

                Thread.Sleep(200);

                return null;
            }

            public string InputQueue { get; private set; }

            public string InputQueueAddress { get { return InputQueue; } }
        }
    }

    class OmgWtfException : Exception
    {
        public OmgWtfException(string omgWtf)
            : base(omgWtf)
        {
        }
    }
}