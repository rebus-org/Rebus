using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Examples;

[TestFixture]
public class DispatchMessageAsDynamic : FixtureBase
{
    [Test]
    public async Task CanDispatchMessageAsDynamic()
    {
        using var activator = new BuiltinHandlerActivator();

        var handledThings = new ConcurrentQueue<dynamic>();

        async Task HandleDynamic(dynamic thing) => handledThings.Enqueue(thing);

        activator.Handle<object>(obj => HandleDynamic((dynamic)obj));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
            .Start();

        await activator.Bus.SendLocal(new SomeRandomMessage("This is the text 🙂"));
        
        await handledThings.WaitUntil(q => q.Count == 1, timeoutSeconds: 2);

        Assert.That(handledThings.Count, Is.EqualTo(1));
        Assert.That(handledThings.First().Text, Is.EqualTo("This is the text 🙂"));
    }

    record SomeRandomMessage(string Text);
}