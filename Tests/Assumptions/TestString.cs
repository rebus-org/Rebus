using NUnit.Framework;

namespace Tests.Assumptions
{
    [TestFixture]
    public class TestString
    {
        [Test]
        public void SplittingCanYieldEmptyTokens()
        {
            var tokens = "=".Split('=');
            Assert.That(tokens.Length, Is.EqualTo(2));
        }
    }
}