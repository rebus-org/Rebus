using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Just a quick test to verify that the syncbus API will enlist in RebusTransactionScope just like all other bus operations")]
public class VerifySyncBusAndTransactionScope : FixtureBase
{
    [Test]
    public void OnlyReceivesPublishedEventWhenRebusTransactionScopeIsCompleted()
    {
        var network = new InMemNetwork();
        var subscriberStore = new InMemorySubscriberStore();

        network.CreateQueue("subscriber");
        subscriberStore.AddSubscriber(typeof(TestEvent).GetSimpleAssemblyQualifiedName(), "subscriber");

        var bus = Configure.With(new BuiltinHandlerActivator())
            .Subscriptions(config => config.StoreInMemory(subscriberStore))
            .Transport(configurer => configurer.UseInMemoryTransport(network, "Test"))
            .Start();

        using (var scope = new RebusTransactionScope())
        {
            bus.Advanced.SyncBus.Publish(new TestEvent("completed"));
            scope.Complete();
        }
            
        using (new RebusTransactionScope())
        {
            bus.Advanced.SyncBus.Publish(new TestEvent("not completed"));
            // this scope is intentionally not completed!
        }

        var messages = network.GetMessages("subscriber").ToList();
        Assert.That(messages.Count, Is.EqualTo(1));

        var transportMessage = messages.First();
        Assert.That(transportMessage.Headers.GetValue(Headers.Type), Is.EqualTo(typeof(TestEvent).GetSimpleAssemblyQualifiedName()));

        var json = Encoding.UTF8.GetString(transportMessage.Body);
        var testEvent = JsonConvert.DeserializeObject<TestEvent>(json);
        Assert.That(testEvent.Label, Is.EqualTo("completed"));
    }

    class TestEvent
    {
        public string Label { get; }

        public TestEvent(string label) => Label = label;
    }
}