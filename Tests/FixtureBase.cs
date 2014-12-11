using NUnit.Framework;

namespace Tests
{
    public abstract class FixtureBase
    {
        [SetUp]
        public void _SetUp()
        {
            SetUp();
        }

        protected virtual void SetUp()
        {
        }

        [TearDown]
        public void _TearDown()
        {
            TearDown();
        }

        protected virtual void TearDown()
        {
        }
    }
}
