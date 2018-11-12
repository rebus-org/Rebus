using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable UnusedMember.Global
#pragma warning disable 1998

namespace Rebus.Tests.Assumptions
{
    [TestFixture]
    public class TestSpecificSerializationExample : FixtureBase
    {
        JsonSerializer _serializer;

        protected override void SetUp()
        {
            _serializer = new JsonSerializer();
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
            using (var activator = new BuiltinHandlerActivator())
            {
                var messageWasReceived = new ManualResetEvent(false);
                DerivedTaskCommand receivedMessage = null;

                activator.Handle<DerivedTaskCommand>(async message =>
                {
                    receivedMessage = message;
                    messageWasReceived.Set();
                });

                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
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

                messageWasReceived.WaitOrDie(TimeSpan.FromSeconds(3));

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
}