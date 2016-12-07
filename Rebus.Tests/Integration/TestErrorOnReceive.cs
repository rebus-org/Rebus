using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestErrorOnReceive : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly ListLoggerFactory _loggerFactory;
        FailToggleTransport _failToggle;

        public TestErrorOnReceive()
        {
            _loggerFactory = new ListLoggerFactory();
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "somequeue"))
                .Logging(l => l.Use(_loggerFactory))
                .Options(o =>
                {
                    o.Decorate<ITransport>(c =>
                    {
                        var transport = c.Get<ITransport>();
                        _failToggle = new FailToggleTransport(transport);
                        return _failToggle;
                    });
                })
                .Start();
        }

        [Fact]
        public void BacksOffWhenExperiencingErrorOnReceive()
        {
            Console.WriteLine("Inducing receive failure...");

            _failToggle.Fail = true;

            Console.WriteLine("Waiting three seconds...");

            Thread.Sleep(5000);

            Console.WriteLine("Lowering fail flag...");

            _failToggle.Fail = false;

            Console.WriteLine("Counting fails");

            var warnings = _loggerFactory.Count(l => l.Level == LogLevel.Warn);

            Assert.True(warnings < 20); //< used to get 60k here
        }

        class FailToggleTransport : ITransport
        {
            readonly ITransport _transport;

            public FailToggleTransport(ITransport transport)
            {
                _transport = transport;
            }

            public void CreateQueue(string address)
            {
                _transport.CreateQueue(address);
            }

            public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                return _transport.Send(destinationAddress, message, context);
            }

            public string Address => _transport.Address;

            public bool Fail { get; set; }

            public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
            {
                if (Fail)
                {
                    throw new Exception("THIS IS A FAKE ERROR CAUSED BY HAVING THE FAIL TOGGLE = TRUE");
                }

                return _transport.Receive(context, cancellationToken);
            }
        }
    }
}