using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.SimpleInjector.Tests
{
    [TestFixture]
    public class SimpleInjectorRealContainerTests : RealContainerTests<SimpleInjectorContainerAdapterFactory>
    {
    }
}