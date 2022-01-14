using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Time;

namespace Rebus.Tests.DataBus;

[TestFixture]
public class TestFileSystemDataBusStorage : FixtureBase
{
    FileSystemDataBusStorage _storage;

    protected override void SetUp()
    {
        var directoryPath = Path.Combine(TestConfig.DirectoryPath(), "databustest");

        DeleteHelper.DeleteDirectory(directoryPath);

        _storage = new FileSystemDataBusStorage(directoryPath, new ConsoleLoggerFactory(false), new FakeRebusTime());
        _storage.Initialize();
    }

    [Test]
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
            Assert.Fail($@"Caught {caughtExceptions.Count} exceptions - here's the first 5:

{string.Join(Environment.NewLine + Environment.NewLine, caughtExceptions.Take(5))}");       
        }
    }
}