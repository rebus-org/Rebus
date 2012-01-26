using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Transports.Rabbit
{
    public class RabbitMqFixtureBase
    {
        protected const string ConnectionString = "amqp://guest:guest@localhost";

        static RabbitMqFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            
        }
        
        [TearDown]
        public void TearDown()
        {
            
        }
    }
}