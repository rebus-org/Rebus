using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestErrorsInPipeline : FixtureBase
    {
        BuiltinHandlerActivator _adapter;
        InMemNetwork _network;
        ListLoggerFactory _listLoggerFactory;

        protected override void SetUp()
        {
            _listLoggerFactory = new ListLoggerFactory();
            _adapter = new BuiltinHandlerActivator();
            _network = new InMemNetwork(outputEventsToConsole: true);

            RebusLoggerFactory.Current = _listLoggerFactory;

            var bus = Configure.With(_adapter)
                .Transport(t => t.UseInMemoryTransport(_network, "test"))
                .Options(o => o.SimpleRetryStrategy("error", 3))
                .Start();

            Using(bus);
        }

        [Test]
        public async Task IncomingMessageHasNoHeaders()
        {
            var gotMessage = false;

            _adapter.Handle<string>(async str => gotMessage = true);

            var body = BodyWith("hej med dig min ven");
            var headersWithoutMessageId = new Dictionary<string, string>();

            _network.Deliver("test", new TransportMessage(headersWithoutMessageId, body).ToInMemTransportMessage());

            await Task.Delay(1000);

            PrintLogs();

            Assert.That(gotMessage, Is.False, "Did not expect to receive the message");

            var loggedErrors = _listLoggerFactory
                .Where(l => l.Level == LogLevel.Error)
                .ToList();

            Assert.That(loggedErrors.Count, Is.EqualTo(1));

            var errorLogLine = loggedErrors.Single(e => e.Level == LogLevel.Error);

            Assert.That(errorLogLine.Text, Contains.Substring(string.Format("Received message with empty or absent '{0}' header", Headers.MessageId)));
        }

        [Test]
        public async Task IncomingMessageCannotBeDeserialized()
        {
            var gotMessage = false;

            _adapter.Handle<string>(async str => gotMessage = true);

            var messageId = Guid.NewGuid();

            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, messageId.ToString()}
            };

            _network.Deliver("test", new TransportMessage(headers, BodyWith("hej igen!")).ToInMemTransportMessage());

            await Task.Delay(1000);

            PrintLogs();

            Assert.That(gotMessage, Is.False, "Did not expect to receive the message");

            var loggedErrors = _listLoggerFactory
                .Where(l => l.Level == LogLevel.Error)
                .ToList();

            Assert.That(loggedErrors.Count, Is.EqualTo(1));

            var errorLogLine = loggedErrors.Single(e => e.Level == LogLevel.Error);

            Assert.That(errorLogLine.Text, Contains.Substring(string.Format("Moving message with ID {0} to error queue 'error'", messageId)));
        }

        void PrintLogs()
        {
            Console.WriteLine("----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, _listLoggerFactory.Select(line => line.ToString().Limit(150))));
            Console.WriteLine("----------------------------------------------------------------------------------------");
        }

        static byte[] BodyWith(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }
}