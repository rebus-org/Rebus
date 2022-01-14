using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.FailFast;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleNamedExpression
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

// ReSharper disable ArgumentsStyleLiteral
[TestFixture]
public class TestFailFastWhenMessageCannotBeDispatchedToAnyHandlers : FixtureBase
{
    BuiltinHandlerActivator _activator;
    ListLoggerFactory _loggerFactory;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _loggerFactory = new ListLoggerFactory(outputToConsole: true);

        Configure.With(_activator)
            .Logging(l => l.Use(_loggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "only-try-once"))
            .Start();
    }

    [Test]
    public async Task OnlyDeliversMessageOnceWhenThereIsNoHandlerForIt()
    {
        _activator.Bus.Advanced.SyncBus.SendLocal("hej med dig din gamle hængerøv");

        await Task.Delay(TimeSpan.FromSeconds(2));

        var numberOfWarnings = _loggerFactory.Count(l => l.Level == LogLevel.Warn);
        var numberOfErrors = _loggerFactory.Count(l => l.Level == LogLevel.Error);

        Assert.That(numberOfWarnings, Is.EqualTo(1), "Expected onle one single WARNING, because the delivery should not be retried");
        Assert.That(numberOfErrors, Is.EqualTo(1), "Expected an error message saying that the message is moved to the error queue");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetFailFastLog(bool failFast)
    {
        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async str => throw new DomainException());

        Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "fail-fast-log-tjek"))
            .Options(o =>
            {
                if (failFast)
                {
                    o.Decorate<IFailFastChecker>(c => new MyFailFastChecker(c.Get<IFailFastChecker>()));
                }
            })
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG");

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    class MyFailFastChecker : IFailFastChecker
    {
        readonly IFailFastChecker _failFastChecker;

        public MyFailFastChecker(IFailFastChecker failFastChecker)
        {
            _failFastChecker = failFastChecker;
        }

        public bool ShouldFailFast(string messageId, Exception exception)
        {
            switch (exception)
            {
                // fail fast on our domain exception
                case DomainException _: return true;

                // delegate all other behavior to default
                default: return _failFastChecker.ShouldFailFast(messageId, exception);
            }
        }
    }

}

public class DomainException : Exception { }