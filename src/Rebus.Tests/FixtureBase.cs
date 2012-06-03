using NUnit.Framework;
using Rebus.Logging;
using Rhino.Mocks;
using log4net.Config;

namespace Rebus.Tests
{
    public abstract class FixtureBase
    {
        static FixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();
            FakeMessageContext.Reset();
            RebusLoggerFactory.Reset();
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected T Mock<T>() where T : class
        {
            return MockRepository.GenerateMock<T>();
        }
    }
}