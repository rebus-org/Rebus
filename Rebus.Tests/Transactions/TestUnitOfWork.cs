using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Transactions
{
    [TestFixture, Description("Verifies that Rebus can sensibly handle common stuff in a unit of work")]
    public class TestUnitOfWork : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        ListLoggerFactory _listLoggerFactory;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());
            _listLoggerFactory = new ListLoggerFactory();

            _bus = Configure.With(_activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "uow"))
                .Start();
        }

        [Test]
        public async Task HandlesExceptionOnCommitAsOrdinaryException()
        {
            _activator.Handle<string>(async str =>
            {
                MessageContext.Current.TransactionContext.OnCommitted(async () =>
                {
                    throw new ConcurrencyException();
                });
            });

            await _bus.SendLocal("hej!");

            await Task.Delay(1000);

            var lines = _listLoggerFactory.ToList();

            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, lines.Select(line => line.ToString().Limit(200))));
            Console.WriteLine("------------------------------------------------------------------------------------------");

            var warnings = lines.Count(l => l.Level == LogLevel.Warn);
            var errors = lines.Count(l => l.Level == LogLevel.Error);

            Assert.That(warnings, Is.EqualTo(5));
            Assert.That(errors, Is.EqualTo(1));
        }

        class ConcurrencyException : Exception { }
    }
}