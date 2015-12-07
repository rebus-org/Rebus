using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Transport.InMem;

namespace Rebus.NLog.Tests
{
    [TestFixture]
    public class ContextVariableWorks : FixtureBase
    {
        [Test]
        public void IncludesCorrelationIdInTheThreeLoggedLines()
        {
            // ${basedir}/logs/logfile.log
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "logfile.log");
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            var activator = new BuiltinHandlerActivator();

            Configure.With(Using(activator))
                .Logging(l => l.NLog())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test"))
                .Start();

            var counter = new SharedCounter(1);

            Using(counter);

            var logger = LogManager.GetLogger("test");

            activator.Handle<string>(async str =>
            {
                logger.Info("1");

                await Task.Delay(100);

                logger.Info("2");

                await Task.Delay(100);

                logger.Info("3");

                counter.Decrement();
            });

            var headers = new Dictionary<string,string>
            {
                {Headers.CorrelationId, "known-correlation-id" }
            };

            activator.Bus.SendLocal("hej med dig min ven!!!", headers).Wait();

            counter.WaitForResetEvent();

            WaitForFile(logFilePath);

            var loggedLines = File.ReadAllLines(logFilePath);

            AssertLineIsThere(loggedLines, "1|known-correlation-id");
            AssertLineIsThere(loggedLines, "2|known-correlation-id");
            AssertLineIsThere(loggedLines, "3|known-correlation-id");

        }

        static void WaitForFile(string logFilePath)
        {
            var waitStart = DateTime.UtcNow;

            while (!File.Exists(logFilePath) && (DateTime.UtcNow - waitStart) < TimeSpan.FromSeconds(10))
            {
                Thread.Sleep(1000);
            }

            Thread.Sleep(1000);
        }

        static void AssertLineIsThere(IEnumerable<string> loggedLines, string expectedLine)
        {
            Assert.That(loggedLines.Any(l => l.Contains(expectedLine)), Is.True,
                @"The expected log line '{0}' was not present:

This is what I found:
---------------------------------------------------------------
{1}
---------------------------------------------------------------

", expectedLine, string.Join(Environment.NewLine, loggedLines));
        }
    }
}
