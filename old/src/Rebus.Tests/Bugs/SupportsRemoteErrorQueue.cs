using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class SupportsRemoteErrorQueue : FixtureBase
    {
        const string InputQueueName = "test.remote.input";

        [Test]
        public void DoesNotChokeWhenErrorQueueIsRemote()
        {
            Configure.With(TrackDisposable(new BuiltinContainerAdapter()))
                .Transport(t => t.UseMsmq(InputQueueName, "thisQueueDoesNotExist@someRandomMachine"))
                .CreateBus()
                .Start();
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            MsmqUtil.Delete(InputQueueName);
        }
    }
}