using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Serialization;
using Rebus.Serialization.Custom;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Examples
{
    [TestFixture]
    public class ShowHowToAvoidSharingMessageDllsAcrossProcesses : FixtureBase
    {
        [Test]
        [Description("Example on how two separate processes can send and receive a message without actually sharing any DLLs")]
        public async Task YeahItWorks()
        {
            using var messageReceived = new ManualResetEvent(initialState: false);

            var network = new InMemNetwork();

            using var sender = Namespace1.BusFactory.CreateSender(
                network: network,
                receiverQueueName: "receiver",
                serializerConfigurationCallback: c => c.UseCustomMessageTypeNames()
                    .AddWithCustomName<Namespace1.SomeMessage>("global.SomeMessage")
            );

            using var _ = Namespace2.BusFactory.CreateReceiver(
                network: network,
                queueName: "receiver",
                messageReceived: messageReceived,
                serializerConfigurationCallback: c => c.UseCustomMessageTypeNames()
                    .AddWithCustomName<Namespace2.SomeMessageSlightlyAltered>("global.SomeMessage")
            );

            await sender.Send(new Namespace1.SomeMessage(Text: "hello there how u doin?"));

            messageReceived.WaitOrDie(TimeSpan.FromSeconds(5), 
                errorMessage: $@"Did not receive the sent {typeof(Namespace1.SomeMessage)} deserialized as {typeof(Namespace2.SomeMessageSlightlyAltered)} withing the 5 s timeout. 

This is most like a sign that the message could not be deserialized.");
        }
    }
}

/*
 * Two separate namespaces containing the same message model: SomeMessage
 */

namespace Namespace1
{
    public record SomeMessage(string Text);

    public static class BusFactory
    {
        public static IBus CreateSender(InMemNetwork network, string receiverQueueName, Action<StandardConfigurer<ISerializer>> serializerConfigurationCallback)
        {
            return Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Routing(r => r.TypeBased().MapFallback(receiverQueueName)) //< just route all messages to the receiver
                .Serialization(serializerConfigurationCallback)
                .Start();
        }
    }
}

namespace Namespace2
{
    public record SomeMessageSlightlyAltered(string Text);

    public static class BusFactory
    {
        public static IBus CreateReceiver(InMemNetwork network, string queueName, ManualResetEvent messageReceived, Action<StandardConfigurer<ISerializer>> serializerConfigurationCallback)
        {
            var activator = new BuiltinHandlerActivator();

            activator.Handle<SomeMessageSlightlyAltered>(async _ => messageReceived.Set());

            return Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, queueName))
                .Serialization(serializerConfigurationCallback)
                .Start();
        }
    }
}