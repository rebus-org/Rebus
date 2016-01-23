using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Handlers;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class Assumptions
    {
        [Test, Ignore("I thought Windsor could resolve by contravariant interface")]
        public void CanResolveByContravariantHandlerInterface()
        {
            var container = new WindsorContainer();
            container.Register(Component.For<IHandleMessages<object>>().ImplementedBy<Handler>());

            var stringHandlers = container.ResolveAll<IHandleMessages<string>>();
            
            Assert.That(stringHandlers.Length, Is.EqualTo(1));
        }

        class Handler : IHandleMessages<object>
        {
            public Task Handle(object message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}