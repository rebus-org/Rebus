using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture, Description("Verifies that Rebus does not fail in horrible ways when encountering unserializable exceptions")]
public class TestUnserializableException : FixtureBase
{
    [Test]
    public async Task CanMoveMessageToErrorQueueEvenThoughExceptionIsNotSerializable()
    {
        using (var activator = new BuiltinHandlerActivator())
        {
            activator.Handle<string>(async str =>
            {
                throw new ThisOneCannotBeSerialized("BAM!!!!!!!!!!!11111111111111111");
            });

            var network = new InMemNetwork();

            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "unserializable exceptions"))
                .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: 1))
                .Start();

            const string knownString = "JUST SOME LISP!!!!11((((((((((((((((((()))))))))))))))))))))))";

            await bus.SendLocal(knownString);

            var failedMessage = await network.WaitForNextMessageFrom("error");

            Assert.That(Encoding.UTF8.GetString(failedMessage.Body), Is.EqualTo(JsonConvert.SerializeObject(knownString)));
        }
    }

    class ThisOneCannotBeSerialized : Exception
    {
        // because it lacks [Serializable] attribute and serialization ctor: ThisOneCannotBeSerialized(SerializationInfo info, StreamingContext context)
        // and because it has a weird object on it


        public ThisOneCannotBeSerialized(string message) : base(message)
        {

        }

        public SomeWeirdObject WeirdObject { get; } = new SomeWeirdObject();
    }

    class SomeWeirdObject
    {
        readonly SomeWeirdObject _circularReference;

        public SomeWeirdObject()
        {
            _circularReference = this;
        }
    }
}