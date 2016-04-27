using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Extensions;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Integration.Sql
{
    [TestFixture, Category(Categories.SqlServer)]
    public class NativeDeferTest : FixtureBase
    {
        static readonly string QueueName = TestConfig.QueueName("input");
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            SqlTestHelper.DropTable("Messages");

            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _bus = Configure.With(_activator)
                .Transport(t => t.UseSqlServer(SqlTestHelper.ConnectionString, "Messages", QueueName))
                .Options(o =>
                {
                    o.LogPipeline();
                })
                .Start();
        }

        [Test]
        public async Task UsesNativeDeferralMechanism()
        {
            var done = new ManualResetEvent(false);
            var receiveTime = DateTimeOffset.MinValue;
            var hadDeferredUntilHeader = false;

            _activator.Handle<TimedMessage>(async (bus, context, message) =>
            {
                receiveTime = DateTimeOffset.Now;

                hadDeferredUntilHeader = context.TransportMessage.Headers.ContainsKey(Headers.DeferredUntil);

                done.Set();
            });

            var sendTime = DateTimeOffset.Now;

            await _bus.Defer(TimeSpan.FromSeconds(5), new TimedMessage { Time = sendTime });

            done.WaitOrDie(TimeSpan.FromSeconds(8), "Did not receive 5s-deferred message within 8 seconds of waiting....");

            var delay = receiveTime - sendTime;

            Console.WriteLine("Message was delayed {0}", delay);

            Assert.That(delay, Is.GreaterThan(TimeSpan.FromSeconds(4)), "The message not delayed ~5 seconds as expected!");
            Assert.That(delay, Is.LessThan(TimeSpan.FromSeconds(8)), "The message not delayed ~5 seconds as expected!");

            Assert.That(hadDeferredUntilHeader, Is.False, "Received message still had the '{0}' header - we must remove that", Headers.DeferredUntil);
        }

        class TimedMessage
        {
            public DateTimeOffset Time { get; set; }
        }
    }
}