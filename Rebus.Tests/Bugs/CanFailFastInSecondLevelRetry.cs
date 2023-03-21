using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class CanFailFastInSecondLevelRetry : FixtureBase
{
    [Test]
    public async Task YesWeCan()
    {
        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async _ => throw new ArgumentException("1st"));
        activator.Handle<IFailed<string>>(async _ => throw new ArgumentException("2nd"));

        var loggerFactory = new ListLoggerFactory(outputToConsole: true);

        var bus = Configure.With(activator)
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
            .Options(o =>
            {
                o.SimpleRetryStrategy(secondLevelRetriesEnabled: true);
                o.FailFastOn<ArgumentException>();
            })
            .Start();

        await bus.SendLocal("HEJ MED DIG!");

        // wait until an error is logged
        await loggerFactory.WaitUntil(lines => lines.Any(l => l.Level == LogLevel.Error));

        // provide extra time for additional stuff to happen
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        var warnings = loggerFactory.Where(l => l.Level == LogLevel.Warn).ToList();

        Assert.That(warnings.Count, Is.EqualTo(1),
            "Expected only one WARNing, because the fail-fast exception should cause it to be marked as FINAL");

        Assert.That(warnings.First().Text, Contains.Substring("FINAL"),
            "Expected the single WARNing to contain the substring 'FINAL'");

        Assert.That(loggerFactory.Count(l => l.Level == LogLevel.Error), Is.EqualTo(1),
            "Expected exactly one ERROR, because the message is only dead-lettered once");
    }
}