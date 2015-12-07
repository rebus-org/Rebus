using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.LightInject.Tests
{
    [TestFixture]
    public class LightInjectRealContainerTests : RealContainerTests<LightInjectContainerAdapterFactory>
    {
    }
}
