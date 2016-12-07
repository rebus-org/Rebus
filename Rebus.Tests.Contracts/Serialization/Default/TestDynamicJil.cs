using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Serialization.Default
{
    public class TestDynamicCapabilityOfDefaultSerializer : FixtureBase
    {
        const string InputQueueName = "json";
        BuiltinHandlerActivator _builtinHandlerActivator;
        InMemNetwork _network;

        public TestDynamicCapabilityOfDefaultSerializer()
        {
            _builtinHandlerActivator = new BuiltinHandlerActivator();

            Using(_builtinHandlerActivator);

            _network = new InMemNetwork();

            Configure.With(_builtinHandlerActivator)
                .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
                .Start();
        }

        [Fact]
        public void DispatchesDynamicMessageWhenDotNetTypeCannotBeFound()
        {
            var gotTheMessage = new ManualResetEvent(false);

            string messageText = null;

            _builtinHandlerActivator.Handle<dynamic>(async message =>
            {
                Console.WriteLine("Received dynamic message: {0}", message);

                messageText = message.something.text;

                gotTheMessage.Set();
            });

            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()},
                {Headers.ContentType, "application/json;charset=utf-8"},
            };

            var transportMessage = new TransportMessage(headers, Encoding.UTF8.GetBytes(@"{
    ""something"": {
        ""text"": ""OMG dynamic JSON BABY!!""
    }
}"));
            _network.Deliver(InputQueueName, new InMemTransportMessage(transportMessage));

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(2));

            Assert.Equal("OMG dynamic JSON BABY!!", messageText);
        }
    }
}