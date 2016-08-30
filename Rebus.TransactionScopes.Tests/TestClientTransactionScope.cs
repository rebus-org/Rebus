using System;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Contracts;
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

        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        public async Task SendsMessageOnlyWhenTransactionScopeIsCompleted(bool completeTheScope, bool throwException, bool expectToReceiveMessage)
        {
            var gotMessage = false;
            _activator.Handle<string>(async str => gotMessage = true);

            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    scope.EnlistRebus();

                    await _bus.SendLocal("hallå i stuen!1");

                    if (throwException)
                    {
                        throw new ApplicationException("omg what is this?????");
                    }

                    if (completeTheScope)
                    {
                        scope.Complete();
                    }
                }
            }
            catch(ApplicationException exception) when (exception.Message == "omg what is this?????")
            {
                Console.WriteLine("An exception occurred... quite expected though");
            }

            await Task.Delay(1000);

            Assert.That(gotMessage, Is.EqualTo(expectToReceiveMessage), "Must receive message IFF the tx scope is completed");
        }
    }
}