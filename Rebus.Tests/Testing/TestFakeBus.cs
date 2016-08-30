using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Testing;
using Rebus.Testing.Events;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Testing
{
    [TestFixture]
    public class TestFakeBus : FixtureBase
    {
        [Test]
        public void CanClearEventsFromFakeBus()
        {
            var bus = new FakeBus();
            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            bus.Send(commandMessage).Wait();

            bus.Clear();

            Assert.That(bus.Events.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task CheckThatEventsAreProperlyRecorded()
        {
            var bus = new FakeBus();

            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            await bus.Send(commandMessage);

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
            var fakeBus = new FakeBus();

            fakeBus.Send(new MyMessage("woohoo!")).Wait();

            var sentMessagesWithMyGreeting = fakeBus.Events
                .OfType<MessageSent<MyMessage>>()
                .Count(m => m.CommandMessage.Text == "woohoo!");

            Assert.That(sentMessagesWithMyGreeting, Is.EqualTo(1));
        }

        [Test]
        public void CanDoItAll()
        {
            var fakeBus = new FakeBus();

            fakeBus.Send(new MyMessage("send")).Wait();
            fakeBus.SendLocal(new MyMessage("send")).Wait();
            fakeBus.Publish(new MyMessage("send")).Wait();
            fakeBus.Defer(TimeSpan.FromSeconds(10), new MyMessage("send")).Wait();
            fakeBus.Subscribe<MyMessage>().Wait();
            fakeBus.Unsubscribe<MyMessage>().Wait();
        }

        [Test]
        public void CanInvokeCallback()
        {
            var fakeBus = new FakeBus();
            var callbacks = new List<string>();

            fakeBus.On<MessageSent>(e => callbacks.Add($"message sent: {e.CommandMessage}"));

            fakeBus.Send("whatever").Wait();

            Assert.That(callbacks, Is.EqualTo(new[] {"message sent: whatever"}));
        }
    }
}