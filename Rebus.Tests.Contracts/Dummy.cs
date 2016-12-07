using Xunit;

namespace Rebus.Tests.Contracts
{
    public class Dummy
    {
        /// <summary>
        /// NUnit 3.5 FAILS if one accidentally points it towards a DLL that has no test fixtures
        /// </summary>
        [Fact]
        public void MakeNUnit35Happy()
        {
        }
    }
}