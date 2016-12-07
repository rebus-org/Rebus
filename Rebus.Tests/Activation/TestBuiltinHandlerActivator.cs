using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Xunit;

namespace Rebus.Tests.Activation
{
    public class TestBuiltinHandlerActivator : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        public TestBuiltinHandlerActivator()
        {
            _activator = new BuiltinHandlerActivator();
        }

        protected override void TearDown()
        {
            AmbientTransactionContext.Current = null;
            _activator.Dispose();
        }

        [Fact]
        public void CanGetHandlerWithoutArguments()
        {
            _activator.Register(() => new SomeHandler());

            var handlers = _activator.GetHandlers("hej med dig", new DefaultTransactionContext()).Result;

            Assert.IsType<SomeHandler>(handlers.Single());
        }

        [Fact]
        public void CanGetHandlerWithMessageContextArgument()
        {
            _activator.Register(context => new SomeHandler());

            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var handlers = _activator.GetHandlers("hej med dig", transactionContext).Result;

                Assert.IsType<SomeHandler>(handlers.Single());
            }
        }

        [Fact]
        public void CanGetHandlerWithBusAndMessageContextArgument()
        {
            _activator.Register((bus, context) => new SomeHandler());

            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var handlers = _activator.GetHandlers("hej med dig", transactionContext).Result;

                Assert.IsType<SomeHandler>(handlers.Single());
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