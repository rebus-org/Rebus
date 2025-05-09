using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Just demonstrates how to configure the due timeouts poll interval")]
public class CustomizeDueTimeoutsPollInterval : FixtureBase
{
    [Test]
    public void DemonstratesHowToCustomizeTheDueTimeoutsPollInterval()
    {
        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Timeouts(t => t.StoreInMemory())
            .Options(o => o.SetDueTimeoutsPollInterval(TimeSpan.FromMinutes(1)))
            .Start();
    }
}