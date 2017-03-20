using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Testing;
using Rebus.Testing.Events;
using Rebus.Tests.Contracts;
#pragma warning disable 1998

namespace Rebus.Tests.Testing
{
    [TestFixture]
    public class TestFakeSyncBus : FixtureBase
    {
        [Test]
        public void CanClearEventsFromFakeBus()
        {
            var bus = new FakeSyncBus();
            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            bus.Send(commandMessage);

            bus.Clear();

            Assert.That(bus.Events.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task CheckThatEventsAreProperlyRecorded()
        {
            var bus = new FakeSyncBus();

            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            bus.Send(commandMessage);

            var messageSentEvents = bus.Events.OfType<MessageSent>().ToList();

            Assert.That(messageSentEvents.Count, Is.EqualTo(1));
            Assert.That(messageSentEvents[0].CommandMessage, Is.EqualTo(commandMessage));
        }

        class MyMessage
        {
            public MyMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        [Test]
        public void CodeSampleForComment()
        {
            var fakeBus = new FakeSyncBus();

            fakeBus.Send(new MyMessage("woohoo!"));

            var sentMessagesWithMyGreeting = fakeBus.Events
                .OfType<MessageSent<MyMessage>>()
                .Count(m => m.CommandMessage.Text == "woohoo!");

            Assert.That(sentMessagesWithMyGreeting, Is.EqualTo(1));
        }

        [Test]
        public void CanDoItAll()
        {
            var fakeBus = new FakeSyncBus();

            fakeBus.Send(new MyMessage("send"));
            fakeBus.SendLocal(new MyMessage("send"));
            fakeBus.Publish(new MyMessage("send"));
            fakeBus.Defer(TimeSpan.FromSeconds(10), new MyMessage("send"));
            fakeBus.Subscribe<MyMessage>();
            fakeBus.Unsubscribe<MyMessage>();
        }

        [Test]
        public void CanInvokeCallback()
        {
            var fakeBus = new FakeSyncBus();
            var callbacks = new List<string>();

            fakeBus.On<MessageSent>(e => callbacks.Add($"message sent: {e.CommandMessage}"));

            fakeBus.Send("whatever");

            Assert.That(callbacks, Is.EqualTo(new[] {"message sent: whatever"}));
        }
    }
}