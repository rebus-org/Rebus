using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.FileSystem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestFileSystemBasedTransportAndCompetingConsumers : FixtureBase
    {
        [Test]
        public void VerifyAssumptionRegardingFileLocks()
        {
            var tempFilePath = GetTempFilePath();

            Assert.That(File.Exists(tempFilePath), Is.True, $"Weird - the file {tempFilePath} could not be found");

            var fileStreamWithLock = Using(File.Open(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete));

            File.Delete(tempFilePath);


        }

        [TestCase(10, 1)]
        [TestCase(10, 2)]
        [TestCase(10, 5)]
        [TestCase(100, 5)]
        [TestCase(1000, 5)]
        public async Task ItWorks(int messageCount, int consumers)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Since file lock currently cannot be created safely in C# on linux, this FileSystemTransport is not supported
                return;
            }
            var tempDirectory = NewTempDirectory();

            var client = Configure.With(Using(new BuiltinHandlerActivator()))
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseFileSystemAsOneWayClient(tempDirectory))
                .Routing(r => r.TypeBased().Map<string>("consumer"))
                .Start();

            var messages = new HashSet<string>(Enumerable.Range(0, messageCount).Select(o => $"THIS IS MESSAGE {o}"));
            var messageCounts = new ConcurrentDictionary<string, int>();

            consumers.Times(() => StartConsumer(messageCounts, tempDirectory));

            foreach (var message in messages)
            {
                await client.Send(message);
            }

            await messageCounts.WaitUntil(d => d.Count >= messageCount, timeoutSeconds: 10);
            // wait 1 extra second for unexpected messages to arrive...
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.That(messages.OrderBy(m => m), Is.EqualTo(messageCounts.Keys.OrderBy(m => m)));
            Assert.That(messageCounts.All(c => c.Value == 1), Is.True, $@"Not all message counts were exactly 0:

{string.Join(Environment.NewLine, messageCounts.Where(kvp => kvp.Value > 1).Select(kvp => $"    {kvp.Key}: {kvp.Value}"))}
");

        }

        void StartConsumer(ConcurrentDictionary<string, int> messageCounts, string tempDirectory)
        {
            var activator = Using(new BuiltinHandlerActivator());

            activator.Handle<string>(async message => messageCounts.AddOrUpdate(message, 1, (key, value) => value + 1));

            Configure.With(activator)
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseFileSystem(tempDirectory, "consumer").Prefetch(5))
                .Start();
        }
    }
}