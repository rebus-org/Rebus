using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.Simple;
using Rebus.Tests;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.UnitOfWork.Tests
{
    [TestFixture]
    public class TestUnitOfWorkDoesNotFailWhenNoHandlerIsProvided : FixtureBase
    {
        const string UowQueueName = "uow-test";

        ConcurrentQueue<string> _events;
        BuiltinHandlerActivator _uowActivator;
        ListLoggerFactory _loggerFactory;

        protected override void SetUp()
        {
            var network = new InMemNetwork();

            _events = new ConcurrentQueue<string>();
            _uowActivator = new BuiltinHandlerActivator();

            Using(_uowActivator);

            _loggerFactory = new ListLoggerFactory(outputToConsole:true);

            Configure.With(_uowActivator)
                .Logging(l => l.Use(_loggerFactory))
                .Transport(t => t.UseInMemoryTransport(network, UowQueueName))
                .Options(o =>
                {
                    o.EnableUnitOfWork(async c => _events, commitAction: async (c, e) => {});
                    o.SimpleRetryStrategy(maxDeliveryAttempts: 1);
                })
                .Start();
        }

        [Test]
        public async Task DoesNotFailWhenNoAbortOrCleanupHandlerIsAdded()
        {
            _uowActivator.Handle<string>(async str => { });

            _uowActivator.Bus.SendLocal("hej med dig min ven!!").Wait();

            Thread.Sleep(2000);

            var logLinesAboveInfo = _loggerFactory.Where(l => l.Level > LogLevel.Info).ToList();

            Assert.That(logLinesAboveInfo.Any(), Is.False, "Got the following: {0}", string.Join(Environment.NewLine, logLinesAboveInfo));
        }
    }
}
