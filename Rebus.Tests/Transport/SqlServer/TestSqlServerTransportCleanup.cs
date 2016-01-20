using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Extensions;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture]
    public class TestSqlServerTransportCleanup : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        ListLoggerFactory _loggerFactory;

        protected override void SetUp()
        {
            var queueName = TestConfig.QueueName("connection_timeout");

            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _loggerFactory = new ListLoggerFactory(outputToConsole: true);

            Configure.With(_activator)
                .Logging(l => l.Use(_loggerFactory))
                .Transport(t => t.UseSqlServer(SqlTestHelper.ConnectionString, "Messages", queueName))
                .Start();
        }

        [Test]
        public void DoesNotBarfInTheBackground()
        {
            var doneHandlingMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                for (var count = 0; count < 5; count++)
                {
                    Console.WriteLine("waiting...");
                    await Task.Delay(TimeSpan.FromSeconds(20));
                }

                Console.WriteLine("done waiting!");

                doneHandlingMessage.Set();
            });

            _activator.Bus.SendLocal("hej med dig min ven!").Wait();

            doneHandlingMessage.WaitOrDie(TimeSpan.FromMinutes(2));

            var logLinesAboveInformation = _loggerFactory
                .Where(l => l.Level >= LogLevel.Warn)
                .ToList();

            Assert.That(!logLinesAboveInformation.Any(), "Expected no warnings - got this: {0}", string.Join(Environment.NewLine, logLinesAboveInformation));
        }
    }
}