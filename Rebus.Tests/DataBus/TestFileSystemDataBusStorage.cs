using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Xunit;

namespace Rebus.Tests.DataBus
{
    public class TestFileSystemDataBusStorage : FixtureBase
    {
        FileSystemDataBusStorage _storage;

        public TestFileSystemDataBusStorage()
        {
            var directoryPath = Path.Combine(AppContext.BaseDirectory, "databustest");

            DeleteHelper.DeleteDirectory(directoryPath);

            _storage = new FileSystemDataBusStorage(directoryPath, new ConsoleLoggerFactory(false));
            _storage.Initialize();
        }

        [Fact]
        public async Task FixFileLockingIssuesWhenUpdatingLastReadTime()
        {
            const string knownId = "known-id";

            Console.WriteLine("Saving some data");
            await _storage.Save(knownId, new MemoryStream(Encoding.UTF8.GetBytes("hej med dig min ven!")));

            var caughtExceptions = new ConcurrentQueue<Exception>();

            Console.WriteLine("Reading the data many times in parallel");
            var threads = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    var thread = new Thread(() =>
                    {
                        10.Times(() =>
                        {
                            try
                            {
                                using (var source = _storage.Read(knownId).Result)
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
                Assert.True(false, $@"Caught {caughtExceptions.Count} exceptions - here's the first 5:
                    {string.Join(Environment.NewLine + Environment.NewLine, caughtExceptions.Take(5))}");
            }
        }
    }
}