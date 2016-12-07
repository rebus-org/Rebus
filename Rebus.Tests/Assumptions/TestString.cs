using Xunit;

namespace Rebus.Tests.Assumptions
{
    public class TestString
    {
        [Fact]
        public void SplittingCanYieldEmptyTokens()
        {
            var tokens = "=".Split('=');
            Assert.Equal(2, tokens.Length);
        }
    }
}