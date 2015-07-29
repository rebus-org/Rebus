using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;
using Rebus.Logging;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class RabbitMqConnectionDoesNotChokeOnSingleTooLargeMessage : FixtureBase
    {
        const string InputQueueName = "test.too.large";
        BuiltinContainerAdapter adapter;
        IBus bus;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();

            Configure.With(adapter)
                     .Logging(l => l.ColoredConsole(LogLevel.Warn))
                     .Transport(t => t.UseRabbitMq(RabbitMqFixtureBase.ConnectionString, InputQueueName, "error")
                                      .ManageSubscriptions()
                                      .PurgeInputQueue())
                     .CreateBus()
                     .Start();

            bus = adapter.Bus;
            bus.Subscribe<byte[]>();
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();
        }

        [TestCase(128)]
        [TestCase(1024)]
        [TestCase(8196)]
        [TestCase(32768)]
        [TestCase(65536, Ignore = true, Description = "kills the Rabbit")]
        [TestCase(65536 * 2, Ignore = true, Description = "kills the Rabbit")]
        [TestCase(65536 * 3, Ignore = true, Description = "kills the Rabbit")]
        [TestCase(65536 * 4, Ignore = true, Description = "kills the Rabbit")]
        [TestCase(65536 * 5, Ignore = true, Description = "kills the Rabbit")]
        public void CanSendAndReceiveMessageWithThisManyKiloBytesOfOPayload(int sizeInKiloBytes)
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<byte[]>(b => resetEvent.Set());

            var sizeInBytes = sizeInKiloBytes * 1024;
            var byteArray = Enumerable.Repeat((byte)'*', sizeInBytes).ToArray();

            Console.WriteLine("Publishing byte[] with Length={0}", byteArray.Length);

            // act
            var stopwatch = Stopwatch.StartNew();

            bus.Publish(byteArray);

            Console.WriteLine("Publishing took {0:0.0} s", stopwatch.Elapsed.TotalSeconds);

            var timeout = 2.Seconds() + TimeSpan.FromMilliseconds(byteArray.Length / 10);
            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not receive {0} bytes of messaging goodness within {1} timeout",
                    sizeInBytes, timeout);
            }

            // assert
        }

        [TestCase(32768 + 1, Description = "Too big => InvalidOperationException inside ApplicationException")]
        public void ThrowsWhenSendingTooHugeMessages(int sizeInKiloBytes)
        {
            var sizeInBytes = sizeInKiloBytes * 1024;
            var byteArray = Enumerable.Repeat((byte)'*', sizeInBytes).ToArray();

            Console.WriteLine("Publishing byte[] with Length={0}", byteArray.Length);

            Assert.Throws<ApplicationException>(() => bus.SendLocal(byteArray));
        }
    }
}