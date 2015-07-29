using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("After message audit was introduced, the attempt to initialize audit from XML resulted in an error if there was not configuration section at all")]
    public class CouldNotStartWithoutXml : FixtureBase
    {
        const string InputQueueName = "noxmlplease.input";

        [Test]
        public void VerifyItIsNotSo()
        {
            using (AppConfig.Change("Bugs\\Cfg\\empty-app.config"))
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                    .Transport(t => t.UseMsmq(InputQueueName, "error"))
                    .CreateBus()
                    .Start();
            }
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(InputQueueName);
        }
    }
}