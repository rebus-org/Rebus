using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.SqlServer;
#pragma warning disable 1998

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class TestSqlTransportReceivePerformance : FixtureBase
    {
        BuiltinHandlerActivator _adapter;

        const string QueueName = "perftest";

        static readonly string TableName = TestConfig.GetName("Messages");

        static readonly string IndexCreationScriptToCheck = $@"
CREATE INDEX IX_{TableName.ToUpperInvariant()}_ID
    ON [dbo].[{TableName}] ([id]) 
    INCLUDE ([recipient], [priority])
";

        protected override void SetUp()
        {
            SqlTestHelper.DropTable(TableName);

            _adapter = Using(new BuiltinHandlerActivator());

            Configure.With(_adapter)
                .Logging(l => l.ColoredConsole(LogLevel.Warn))
                .Transport(t => t.UseSqlServer(SqlTestHelper.ConnectionString, TableName, QueueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(20);
                })
                .Start();
        }

        [TestCase(1000, true, false)]
        [TestCase(1000, true, true)]
        [TestCase(1000, false, false)]
        [TestCase(10000, true, false, Ignore = "run manually")]
        [TestCase(10000, true, true, Ignore= "run manually")]
        [TestCase(10000, false, false, Ignore= "run manually")]
        public async Task NizzleName(int messageCount, bool useNewIndexScript, bool dropOldIndex)
        {
            if (useNewIndexScript)
            {
                SqlTestHelper.Execute(IndexCreationScriptToCheck);

                SqlTestHelper.DropIndex(TableName, $"IDX_RECEIVE_{TableName}");
            }

            Console.WriteLine($"Sending {messageCount} messages...");

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => _adapter.Bus.SendLocal($"THIS IS MESSAGE {i}")));

            var counter = new SharedCounter(messageCount);

            _adapter.Handle<string>(async message => counter.Decrement());

            Console.WriteLine("Waiting for messages to be received...");

            var stopwtach = Stopwatch.StartNew();

            _adapter.Bus.Advanced.Workers.SetNumberOfWorkers(3);

            counter.WaitForResetEvent(messageCount / 500 + 5);

            var elapsedSeconds = stopwtach.Elapsed.TotalSeconds;

            Console.WriteLine($"{messageCount} messages received in {elapsedSeconds:0.0} s - that's {messageCount / elapsedSeconds:0.0} msg/s");
        }
    }
}