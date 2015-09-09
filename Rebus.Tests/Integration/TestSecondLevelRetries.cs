using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSecondLevelRetries : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            _bus = Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "2nd level goodness"))
                .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true))
                .Start();
        }

        [Test]
        public async Task ItWorks()
        {
            var counter = new SharedCounter(1);

            _activator.Handle<string>(async str =>
            {
                throw new ApplicationException("1st level!!");
            });

            _activator.Handle<Failed<string>>(async failed =>
            {
                if (failed.Message != "hej med dig!")
                {
                    counter.Fail("Did not receive the expected message!");
                    return;
                }

                counter.Decrement();
            });

            await _bus.SendLocal("hej med dig!");

            counter.WaitForResetEvent();
        }
    }
}