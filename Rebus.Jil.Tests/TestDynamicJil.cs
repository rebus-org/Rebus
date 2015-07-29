using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Jil.Tests
{
    [TestFixture]
    public class TestDynamicJil : FixtureBase
    {
        const string InputQueueName = "jil-bandit";
        BuiltinHandlerActivator _builtinHandlerActivator;
        InMemNetwork _network;

        protected override void SetUp()
        {
            _builtinHandlerActivator = new BuiltinHandlerActivator();
            
            Using(_builtinHandlerActivator);

            _network = new InMemNetwork();

            Configure.With(_builtinHandlerActivator)
                .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
                .Serialization(s => s.UseJil())
                .Start();
        }

        [Test]
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

            Assert.That(messageText, Is.EqualTo("OMG dynamic JSON BABY!!"));
        }
    }
}