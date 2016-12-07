using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Testing;
using Rebus.Testing.Events;
using Rebus.Tests.Contracts;
using Xunit;

namespace Rebus.Tests.Testing
{
    public class TestFakeBus : FixtureBase
    {
        [Fact]
        public void CanClearEventsFromFakeBus()
        {
            var bus = new FakeBus();
            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            bus.Send(commandMessage).Wait();

            bus.Clear();

            Assert.Equal(0, bus.Events.Count());
        }

        [Fact]
        public async Task CheckThatEventsAreProperlyRecorded()
        {
            var bus = new FakeBus();

            var commandMessage = new { Text = "hej med dig min ven!!!!" };
            await bus.Send(commandMessage);

            var messageSentEvents = bus.Events.OfType<MessageSent>().ToList();

            Assert.Equal(1, messageSentEvents.Count);
            Assert.Equal(commandMessage, messageSentEvents[0].CommandMessage);
        }

        class MyMessage
        {
            public MyMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        [Fact]
        public void CodeSampleForComment()
        {
            var fakeBus = new FakeBus();

            fakeBus.Send(new MyMessage("woohoo!")).Wait();

            var sentMessagesWithMyGreeting = fakeBus.Events
                .OfType<MessageSent<MyMessage>>()
                .Count(m => m.CommandMessage.Text == "woohoo!");

            Assert.Equal(1, sentMessagesWithMyGreeting);
        }

        [Fact]
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

        [Fact]
        public void CanInvokeCallback()
        {
            var fakeBus = new FakeBus();
            var callbacks = new List<string>();

            fakeBus.On<MessageSent>(e => callbacks.Add($"message sent: {e.CommandMessage}"));

            fakeBus.Send("whatever").Wait();

            Assert.Equal(new[] {"message sent: whatever"}, callbacks);
        }
    }
}