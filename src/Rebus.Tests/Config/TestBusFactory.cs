using NUnit.Framework;
using Rebus.Config;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Config
{
    [TestFixture]
    public class TestBusFactory
    {
        [Test]
        public void ConfiguresTheBusLikeExpected()
        {
            const string connectionString = "data source=.;initial catalog=rebus_test1;integrated security=sspi";

            var bus = BusFactory
                .NewBus(c =>
                            {
                                c.UseMsmqTransport("some_inputQueue");
                                c.UseDbSubscriptionStorage(connectionString);
                            });


        }
    }
}