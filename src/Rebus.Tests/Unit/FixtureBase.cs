using NUnit.Framework;
using Rhino.Mocks;

namespace Rebus.Tests.Unit
{
    public class FixtureBase
    {
        [SetUp]
        public void SetUp()
        {
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        protected T Mock<T>() where T : class
        {
            return MockRepository.GenerateMock<T>();
        }
    }
}