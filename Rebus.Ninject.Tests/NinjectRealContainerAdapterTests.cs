using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Ninject.Tests
{
    [TestFixture]
    public class NinjectRealContainerAdapterTests : RealContainerTests<NinjectContainerAdapterFactory>
    {
    }
}