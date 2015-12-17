using System.Linq;
using System.Threading.Tasks;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Handlers;

namespace Rebus.CastleWindsor.Tests
{
    [TestFixture]
    public class TestAssemblyScanning
    {
        [Test]
        public void PicksUpRandomPrivateHandlerWhenDoingAutoRegistration()
        {
            var container = new WindsorContainer();

            container.AutoRegisterHandlersFromThisAssembly();

            var secretHandlers = container.ResolveAll<IHandleMessages<Secret>>();

            Assert.That(secretHandlers.Length, Is.EqualTo(1));
            Assert.That(secretHandlers.Single(), Is.TypeOf<SecretHandler>());
        }

        class Secret { }

        class SecretHandler : IHandleMessages<Secret>
        {
            public Task Handle(Secret message)
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void CanRegisterSomeHandlerInTheRightWay()
        {
            var container = new WindsorContainer();

            container.RegisterHandler<SomeHandler>();

            var stringHandlers = container.ResolveAll<IHandleMessages<string>>();
            var stringHandlersAgain = container.ResolveAll<IHandleMessages<string>>();

            Assert.That(stringHandlers.Any(h => h is SomeHandler), Is.True,
                "Did not find SomeHandler among the following available handler instances: {0}",
                string.Join(", ", stringHandlers.Select(h => h.GetType().Name)));

            Assert.That(stringHandlers.Intersect(stringHandlersAgain).Any(), Is.False,
                @"Did not expect any instances to be common among

{0}

and

{1}

which were resolved in two separate ResolveAll calls",
                string.Join(", ", stringHandlers.Select(h => h.GetType().Name)),
                string.Join(", ", stringHandlersAgain.Select(h => h.GetType().Name)));
        }

        class SomeHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}