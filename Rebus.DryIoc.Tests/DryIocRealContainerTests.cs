using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.DryIoc.Tests
{
    [TestFixture]
    public class DryIocRealContainerTests : RealContainerTests<DryIocContainerAdapterFactory>
    {
    }
}
