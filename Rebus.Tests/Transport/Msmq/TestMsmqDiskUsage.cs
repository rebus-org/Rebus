using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture, Ignore("to be run manually")]
    public class TestMsmqDiskUsage : FixtureBase
    {
        protected override void SetUp()
        {
            var activator = Using(new BuiltinHandlerActivator());

            Configure.With(activator)
                .Transport(t => t.UseMsmq(TestConfig.QueueName("idle")))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(20);
                    o.SetMaxParallelism(20);
                })
                .Start();
        }

        [Test]
        public void Sleep()
        {
            Thread.Sleep(3000);
        }
    }
}