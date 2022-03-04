using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable UnusedMember.Global
#pragma warning disable 1998

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestSpecificSerializationExample : FixtureBase
{
    JsonSerializer _serializer;

    protected override void SetUp()
    {
        _serializer = new JsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention());
    }

    [Test]
    public async Task ItWorks()
    {
        var command = new DerivedTaskCommand
        {
            Delays =
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(5),
            }
        };

        var roundtrippedCommand = await Roundtrip(command);

        Console.WriteLine($@"{command.ToJson()}

=> 

{roundtrippedCommand.ToJson()}");

        Assert.That(roundtrippedCommand.ToJson(), Is.EqualTo(command.ToJson()));
    }

    [Test]
    public async Task MessageLooksRightWhenReceived()
    {
        using var activator = new BuiltinHandlerActivator();
        var receivedMessages = new ConcurrentQueue<DerivedTaskCommand>();
        activator.Handle<DerivedTaskCommand>(async message => receivedMessages.Enqueue(message));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
            .Serialization(s => s.UseNewtonsoftJson()) //< have to use Newtonsoft here, because the message uses inheritance
            .Start();

        await activator.Bus.SendLocal(new DerivedTaskCommand
        {
            Delays =
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(5),
            }
        });

        var receivedMessage = await receivedMessages.DequeueNext();

        Assert.That(receivedMessage, Is.Not.Null);
        Assert.That(receivedMessage.Delays, Is.EqualTo(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(5),
        }));
    }

    async Task<T> Roundtrip<T>(T obj)
    {
        var message = new Message(new Dictionary<string, string>(), obj);
        var transportMessage = await _serializer.Serialize(message);
        var roundtrippedMessage = await _serializer.Deserialize(transportMessage);
        return (T)roundtrippedMessage.Body;
    }

    abstract class BaseTaskCommand
    {
        public IList<TimeSpan> Delays { get; } = new List<TimeSpan>();
    }

    class DerivedTaskCommand : BaseTaskCommand
    {
        public int TaskId { get; set; }
    }
}