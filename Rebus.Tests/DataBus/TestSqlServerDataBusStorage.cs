using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.DataBus.SqlServer;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.DataBus
{
    [TestFixture]
    public class TestSqlServerDataBusStorage : FixtureBase
    {
        SqlServerDataBusStorage _storage;

        protected override void SetUp()
        {
            var loggerFactory = new ConsoleLoggerFactory(false);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, loggerFactory);

            var tableName = TestConfig.GetName("data");

            SqlTestHelper.DropTable(tableName);

            _storage = new SqlServerDataBusStorage(connectionProvider, tableName, true, loggerFactory);
            _storage.Initialize();
        }

        [Test]
        public async Task CanReadDataInParallel()
        {
            var longString = string.Concat(Enumerable.Repeat(@"

Let me explain something to you. Um, I am not ""Mr.Lebowski"". 

You're Mr. Lebowski. I'm the Dude. 

So that's what you call me. 

You know, that or, uh, His Dudeness, or uh, Duder, or El Duderino if you're not into the whole brevity thing.

", 100));

            const string dataId = "known-id";

            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(longString)))
            {
                await _storage.Save(dataId, source);
            }

            var caughtExceptions = new ConcurrentQueue<Exception>();

            Console.WriteLine("Reading the data many times in parallel");
            var threads = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    var thread = new Thread(() =>
                    {
                        100.Times(() =>
                        {
                            Console.Write(".");
                            try
                            {
                                using (var source = _storage.Read(dataId).Result)
                                using (var destination = new MemoryStream())
                                {
                                    source.CopyTo(destination);
                                }
                            }
                            catch (Exception exception)
                            {
                                caughtExceptions.Enqueue(exception);
                            }
                        });
                    });

                    return thread;
                })
                .ToList();

            Console.WriteLine("Starting threads");
            threads.ForEach(t => t.Start());

            Console.WriteLine("Waiting for them to finish");
            threads.ForEach(t => t.Join());

            Console.WriteLine("Finished :)");

            if (caughtExceptions.Count > 0)
            {
                Assert.Fail($@"Caught {caughtExceptions.Count} exceptions - here's the first 5:

{string.Join(Environment.NewLine + Environment.NewLine, caughtExceptions.Take(5))}");
            }

        }
    }
}