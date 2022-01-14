using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestMinimumRequiredSetOfHeaders : FixtureBase
{
    const string QueueName = "some-queue";
    BuiltinHandlerActivator _activator;
    InMemNetwork _network;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();
        _network = new InMemNetwork();

        Using(_activator);

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(_network, QueueName))
            .Serialization(s => s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
            .Create();
    }

    [Test]
    public void VerifyMessageCanBeConsumed()
    {
        var randomTime = new DateTimeOffset(1979, 3, 19, 14, 00, 00, TimeSpan.FromHours(1));
        var messageLooksGood = new ManualResetEvent(false);

        _activator.Handle<SomeRandomMessage>(async message =>
        {
            if (message.Text == "hej søtte" && message.Time == randomTime)
            {
                messageLooksGood.Set();
            }
            else
            {
                throw new ArgumentException($@"This message:

{message.ToJson()}

did not contain the expected values");
            }
        });
            
        _starter.Start();

        var headers = new Dictionary<string, string>
        {
            {Headers.MessageId, Guid.NewGuid().ToString() },
            {Headers.ContentType, "application/json;charset=utf-8" },
            {Headers.Type, "Rebus.Tests.Assumptions.TestMinimumRequiredSetOfHeaders+SomeRandomMessage, Rebus.Tests" }
        };
        var body = Encoding.UTF8.GetBytes("{text:'hej søtte', time: '1979-03-19T14:00:00+01:00'}");
        var manuallyConstructedTransportMessage = new TransportMessage(headers, body);

        _network.Deliver(QueueName, new InMemTransportMessage(manuallyConstructedTransportMessage));

        messageLooksGood.WaitOrDie(TimeSpan.FromSeconds(200));
    }

    class SomeRandomMessage
    {
        public string Text { get; }
        public DateTimeOffset Time { get; }

        public SomeRandomMessage(string text, DateTimeOffset time)
        {
            Text = text;
            Time = time;
        }
    }
}