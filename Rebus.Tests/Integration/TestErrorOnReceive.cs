using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestErrorOnReceive : FixtureBase
{
    BuiltinHandlerActivator _activator;
    ListLoggerFactory _loggerFactory;
    FailToggleTransport _failToggle;

    protected override void SetUp()
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

    [Test]
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

        Assert.That(warnings, Is.LessThan(20)); //< used to get 60k here
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
                throw new RebusApplicationException("THIS IS A FAKE ERROR CAUSED BY HAVING THE FAIL TOGGLE = TRUE");
            }

            return _transport.Receive(context, cancellationToken);
        }
    }
}