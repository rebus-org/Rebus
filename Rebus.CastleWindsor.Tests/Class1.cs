using Castle.Windsor;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class ContainerTests : ContainerTests<CastleWindsorHandlerActivatorFactory>
    {
    }

    public class CastleWindsorHandlerActivatorFactory : IHandlerActivatorFactory
    {
        readonly WindsorContainer _windsorContainer = new WindsorContainer();

        public IHandlerActivator GetActivator()
        {
            return new CastleWindsorHandlerActivator(_windsorContainer);
        }
    }
}
