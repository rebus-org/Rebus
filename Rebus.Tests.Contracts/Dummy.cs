using NUnit.Framework;
#pragma warning disable CS1591

namespace Rebus.Tests.Contracts;

[TestFixture]
public class Dummy
{
    /// <summary>
    /// NUnit 3.5 FAILS if one accidentally points it towards a DLL that has no test fixtures
    /// </summary>
    [Test]
    public void MakeNUnit350GreatAgain()
    {
    }
}