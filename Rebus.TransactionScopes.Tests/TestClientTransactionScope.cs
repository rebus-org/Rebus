using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.TransactionScopes.Tests
{
    [TestFixture]
    public class TestClientTransactionScope : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "tx-text"))
                .Start();

            _bus = _activator.Bus;
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task SendsMessageOnlyWhenTransactionScopeIsCompleted(bool completeTheScope, bool expectToReceiveMessage)
        {
            var gotMessage = false;
            _activator.Handle<string>(async str => gotMessage = true);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                scope.EnlistRebus();

                await _bus.SendLocal("hallå i stuen!1");

                if (completeTheScope)
                {
                    scope.Complete();
                }
            }

            await Task.Delay(1000);

            Assert.That(gotMessage, Is.EqualTo(expectToReceiveMessage), "Must receive message IFF the tx scope is completed");
        }
    }
}