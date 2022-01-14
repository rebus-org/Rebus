using NUnit.Framework;

namespace Rebus.Tests.Assumptions;

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