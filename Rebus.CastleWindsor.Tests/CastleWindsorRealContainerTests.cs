using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class CastleWindsorRealContainerTests : RealContainerTests<CastleWindsorContainerAdapterFactory>
    {
    }
}