using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Serialization.Default;

[TestFixture]
public class TestDynamicCapabilityOfDefaultSerializer : FixtureBase
{
    const string InputQueueName = "json";
    BuiltinHandlerActivator _builtinHandlerActivator;
    InMemNetwork _network;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _builtinHandlerActivator = new BuiltinHandlerActivator();

        Using(_builtinHandlerActivator);

        _network = new InMemNetwork();

        _starter = Configure.With(_builtinHandlerActivator)
            .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
            .Create();
    }

    [Test]
    public void DispatchesDynamicMessageWhenDotNetTypeCannotBeFound()
    {
        using var gotTheMessage = new ManualResetEvent(false);

        string messageText = null;

        _builtinHandlerActivator.Handle<dynamic>(async message =>
        {
            Console.WriteLine("Received dynamic message: {0}", message);

            messageText = message.something.text;

            gotTheMessage.Set();
        });

        _starter.Start();

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

        gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(5));

        Assert.That(messageText, Is.EqualTo("OMG dynamic JSON BABY!!"));
    }
}