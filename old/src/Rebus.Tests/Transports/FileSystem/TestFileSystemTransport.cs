using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.FileSystem;
using Shouldly;

namespace Rebus.Tests.Transports.FileSystem
{
    [TestFixture]
    public class TestFileSystemTransport : FixtureBase
    {
        const string InputQueueName = "bus1";
        readonly string playDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rebus_messages");

        BuiltinContainerAdapter bus1;
        BuiltinContainerAdapter bus2;

        protected override void DoSetUp()
        {
            bus1 = new BuiltinContainerAdapter();

            Configure.With(TrackDisposable(bus1))
                .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                .Transport(t => t.UseTheFileSystem(playDir, InputQueueName, "error"))
                .CreateBus()
                .Start(10);

            bus2 = new BuiltinContainerAdapter();
            
            Configure.With(TrackDisposable(bus2))
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseTheFileSystemInOneWayClientMode(playDir))
                .CreateBus()
                .Start();
        }

        protected override void DoTearDown()
        {
            if (Directory.Exists(playDir))
                Directory.Delete(playDir, true);
        }

        [Test]
        public void DoesNotReceiveTheSameMessageTwice()
        {
            var counts = new ConcurrentDictionary<string, int>();
            bus1.Handle<string>(str =>
            {
                counts.AddOrUpdate(str, key => 1, (key, value) => value + 1);
                Thread.Sleep(100);
            });

            const string message = "bam!!";
            bus1.Bus.SendLocal(message);

            Thread.Sleep(2.Seconds());

            counts.ShouldContainKeyAndValue(message, 1);
        }

        [Test]
        public void CanSendWithOneWayClient()
        {
            var counts = new ConcurrentDictionary<string, int>();
            bus1.Handle<string>(str =>
            {
                counts.AddOrUpdate(str, key => 1, (key, value) => value + 1);
                Thread.Sleep(100);
            });

            const string message = "bam!!";
            bus2.Bus.Advanced.Routing.Send(InputQueueName, message);

            Thread.Sleep(2.Seconds());

            counts.ShouldContainKeyAndValue(message, 1);
        }

        [Test]
        public void DoesNotReceiveTheSameMessageTwiceAndDoesNotLockEverythinEither()
        {
            var counts = new ConcurrentDictionary<string, int>();
            bus1.Handle<string>(str =>
            {
                counts.AddOrUpdate(str, key => 1, (key, value) => value + 1);
                Thread.Sleep(100);
            });

            Enumerable.Range(0, 100)
                .ToList()
                .ForEach(number => bus1.Bus.SendLocal(number.ToString()));

            Thread.Sleep(4.Seconds());

            counts.Count.ShouldBe(100);
            counts.Values.All(v => v == 1).ShouldBe(true);
        }
    }
}