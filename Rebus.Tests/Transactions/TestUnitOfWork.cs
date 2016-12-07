using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Transactions
{
    // Verifies that Rebus can sensibly handle common stuff in a unit of work
    public class TestUnitOfWork : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly ListLoggerFactory _listLoggerFactory;
        readonly IBus _bus;

        public TestUnitOfWork()
        {
            _activator = Using(new BuiltinHandlerActivator());
            _listLoggerFactory = new ListLoggerFactory();

            _bus = Configure.With(_activator)
                .Logging(l => l.Use(_listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "uow"))
                .Start();
        }

        [Fact]
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

            Assert.Equal(5, warnings);
            Assert.Equal(1, errors);
        }

        class ConcurrencyException : Exception { }
    }
}