using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Transport;

namespace Rebus.Tests.Activation
{
    [TestFixture]
    public class TestBuiltinHandlerActivator : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();
        }

        protected override void TearDown()
        {
            AmbientTransactionContext.SetCurrent(null);

            _activator.Dispose();
        }

        [Test]
        public void CanGetHandlerWithoutArguments()
        {
            _activator.Register(() => new SomeHandler());

            using (var scope = new RebusTransactionScope())
            {
                var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

                Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
            }
        }

        [Test]
        public void CanGetHandlerWithMessageContextArgument()
        {
            _activator.Register(context => new SomeHandler());

            using (var scope = new RebusTransactionScope())
            {
                var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

                Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
            }
        }

        [Test]
        public void CanGetHandlerWithBusAndMessageContextArgument()
        {
            _activator.Register((bus, context) => new SomeHandler());

            using (var scope = new RebusTransactionScope())
            {
                var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

                Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
            }
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