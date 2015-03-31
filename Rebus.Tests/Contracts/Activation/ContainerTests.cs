using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;

namespace Rebus.Tests.Contracts.Activation
{
    public class ContainerTests<TFactory> : FixtureBase where TFactory : IHandlerActivatorFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
        }

        [Test]
        public async Task ResolvingWithoutRegistrationYieldsEmptySequenec()
        {
            var handlerActivator = _factory.GetActivator();

            var handlers = (await handlerActivator.GetHandlers("hej")).ToList();

            Assert.That(handlers.Count, Is.EqualTo(0));
        }
    }

    public interface IHandlerActivatorFactory
    {
        IHandlerActivator GetActivator();
    }
}