using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestErrorsInPipeline : FixtureBase
    {
        BuiltinHandlerActivator _adapter;
        InMemNetwork _network;
        ListLoggerFactory _listLoggerFactory;

        public TestErrorsInPipeline()
        {
            _listLoggerFactory = new ListLoggerFactory();
            _adapter = new BuiltinHandlerActivator();
            _network = new InMemNetwork(outputEventsToConsole: true);

            var bus = Configure.With(_adapter)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(_network, "test"))
                .Options(o => o.SimpleRetryStrategy("error", 3))
                .Start();

            Using(bus);
        }

        [Fact]
        public async Task IncomingMessageHasNoHeaders()
        {
            var gotMessage = false;

            _adapter.Handle<string>(async str => gotMessage = true);

            var body = BodyWith("hej med dig min ven");
            var headersWithoutMessageId = new Dictionary<string, string>();

            _network.Deliver("test", new TransportMessage(headersWithoutMessageId, body).ToInMemTransportMessage());

            await Task.Delay(1000);

            PrintLogs();

            Assert.False(gotMessage, "Did not expect to receive the message");

            var loggedErrors = _listLoggerFactory
                .Where(l => l.Level == LogLevel.Error)
                .ToList();

            Assert.Equal(1, loggedErrors.Count);

            var errorLogLine = loggedErrors.Single(e => e.Level == LogLevel.Error);

            Assert.Contains($"Received message with empty or absent '{Headers.MessageId}' header", errorLogLine.Text);
        }

        [Fact]
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

            Assert.False(gotMessage, "Did not expect to receive the message");

            var loggedErrors = _listLoggerFactory
                .Where(l => l.Level == LogLevel.Error)
                .ToList();

            Assert.Equal(1, loggedErrors.Count);

            var errorLogLine = loggedErrors.Single(e => e.Level == LogLevel.Error);

            Assert.Contains($"Moving message with ID {messageId} to error queue 'error'",errorLogLine.Text);
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