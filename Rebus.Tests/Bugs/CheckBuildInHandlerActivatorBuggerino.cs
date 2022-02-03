using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable RedundantLambdaParameterType
// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Try to reproduce a bug. Turned out there was no bug")]
public class CheckBuildInHandlerActivatorBuggerino : FixtureBase
{
    [Test]
    public async Task CanDoIt()
    {
        using var gotTheEvent = new ManualResetEvent(initialState: false);
        using var activator = new BuiltinHandlerActivator();

        var subscriberStore = new InMemorySubscriberStore();
        var network = new InMemNetwork();

        var starter = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "buggerino"))
            .Subscriptions(s => s.StoreInMemory(subscriberStore))
            .Create();

        var publisher = Configure.With(Using(new BuiltinHandlerActivator()))
            .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
            .Subscriptions(s => s.StoreInMemory(subscriberStore))
            .Start();

        Task Handle(ThisIsTheEventWeAreTalkingAbout configurationUpdatedEvent)
        {
            Console.WriteLine($"Got event: {configurationUpdatedEvent}");
            gotTheEvent.Set();
            return Task.CompletedTask;
        }

        activator.Handle<ThisIsTheEventWeAreTalkingAbout>((ThisIsTheEventWeAreTalkingAbout e) => Handle(e));

        await starter.Bus.Subscribe(typeof(ThisIsTheEventWeAreTalkingAbout));

        starter.Start();

        await publisher.Publish(new ThisIsTheEventWeAreTalkingAbout());

        gotTheEvent.WaitOrDie(timeout: TimeSpan.FromSeconds(2));
    }

    record ThisIsTheEventWeAreTalkingAbout { }

}