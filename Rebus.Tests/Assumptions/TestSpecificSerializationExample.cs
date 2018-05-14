using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Extensions;

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

        async Task<T> Roundtrip<T>(T obj)
        {
            var message = new Message(new Dictionary<string, string>(), obj);
            var transportMessage = await _serializer.Serialize(message);
            var roundtrippedMessage = await _serializer.Deserialize(transportMessage);
            return (T)roundtrippedMessage.Body;
        }

        abstract class BaseTaskCommand
        {
            private IList<TimeSpan> _delays = new List<TimeSpan>();
            public IList<TimeSpan> Delays => _delays;
        }

        class DerivedTaskCommand : BaseTaskCommand
        {
            public int TaskId { get; set; }
        }
    }
}