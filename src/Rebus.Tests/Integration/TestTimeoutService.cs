using System.Threading;
using NUnit.Framework;
using Rebus.Timeout;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestTimeoutService : FixtureBase
    {
        TimeoutService timeoutService;

        protected override void DoSetUp()
        {
            timeoutService = new TimeoutService();
            timeoutService.Start();
        }

        protected override void DoTearDown()
        {
            timeoutService.Stop();
        }

        [Test]
        public void WillCallBackAfterTimeHasElapsed()
        {
            Thread.Sleep(2000);
        }
    }
}