using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Tests.Util;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMessageAudit : FixtureBase, IDetermineMessageOwnership
    {
        const string InputQueueName = "test.audit.input";
        const string AuditQueueName = "test.audit.audit";
        BuiltinContainerAdapter adapter;

        protected override void DoSetUp()
        {
            adapter = TrackDisposable(new BuiltinContainerAdapter());

            MsmqUtil.EnsureMessageQueueExists(MsmqUtil.GetPath(AuditQueueName));
            MsmqUtil.PurgeQueue(AuditQueueName);
            MsmqUtil.PurgeQueue(InputQueueName);

            Configure.With(adapter)
                .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                .Transport(t => t.UseMsmq(InputQueueName, "error"))
                .Behavior(b => b.EnableMessageAudit(AuditQueueName))
                .CreateBus()
                .Start(1);
        }

        static string InputQueueAddress
        {
            get { return InputQueueName + "@" + Environment.MachineName; }
        }

        void SetUpSubscriberThatDoesNotAuditMessages(string inputQueueName)
        {
            var subscriberAdapter = TrackDisposable(new BuiltinContainerAdapter());

            subscriberAdapter.Handle<string>(s => { });

            var bus = Configure.With(subscriberAdapter)
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseMsmq(inputQueueName, "error"))
                .MessageOwnership(o => o.Use(this))
                .CreateBus()
                .Start(1);

            bus.Subscribe<string>();
        }

        [Test]
        public void CanCopySuccessfullyHandledMessageToAuditQueue()
        {
            // arrange
            var fakeTime = DateTime.UtcNow;
            TimeMachine.FixTo(fakeTime);
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<string>(str => resetEvent.Set());

            // act
            adapter.Bus.SendLocal("yo!");
            
            // assert
            var message = MsmqTestHelper.GetMessagesFrom(AuditQueueName).Single();
            
            message.ShouldNotBe(null);

            var logicalMessages = message.Messages;
            var headers = message.Headers;

            logicalMessages.Length.ShouldBe(1);
            logicalMessages[0].ShouldBe("yo!");

            headers.ShouldContainKeyAndValue(Headers.AuditReason, Headers.AuditReasons.Handled);
            headers.ShouldContainKeyAndValue(Headers.AuditMessageCopyTime, fakeTime.ToString("u"));
            headers.ShouldContainKeyAndValue(Headers.AuditSourceQueue, InputQueueAddress);
        }

        [Test]
        public void CanCopyPublishedMessageToAuditQueue()
        {
            // arrange
            var fakeTime = DateTime.UtcNow;
            TimeMachine.FixTo(fakeTime);

            Thread.Sleep(1.Seconds());

            // act
            adapter.Bus.Publish("yo!");
            
            // assert
            var message = MsmqTestHelper.GetMessagesFrom(AuditQueueName).Single();
            
            message.ShouldNotBe(null);

            var logicalMessages = message.Messages;
            var headers = message.Headers;

            logicalMessages.Length.ShouldBe(1);
            logicalMessages[0].ShouldBe("yo!");

            headers.ShouldContainKeyAndValue(Headers.AuditReason, Headers.AuditReasons.Published);
            headers.ShouldContainKeyAndValue(Headers.AuditMessageCopyTime, fakeTime.ToString("u"));
            headers.ShouldContainKeyAndValue(Headers.AuditSourceQueue, InputQueueAddress);
        }

        [Test]
        public void PublishedMessageIsCopiedOnlyOnceRegardlessOfNumberOfSubscribers()
        {
            // arrange
            var fakeTime = DateTime.UtcNow;
            TimeMachine.FixTo(fakeTime);

            SetUpSubscriberThatDoesNotAuditMessages("test.audit.subscriber1");
            SetUpSubscriberThatDoesNotAuditMessages("test.audit.subscriber2");
            SetUpSubscriberThatDoesNotAuditMessages("test.audit.subscriber3");

            // act
            adapter.Bus.Publish("yo!");
            
            // assert
            var messages = MsmqTestHelper.GetMessagesFrom(AuditQueueName).ToList();

            Console.WriteLine(string.Join(Environment.NewLine, messages));

            messages.Count.ShouldBe(4);

            var stringMessages = messages.Where(m => m.Messages[0] is string).ToList();

            Assert.That(stringMessages.Count, Is.EqualTo(1), "We should have only one single copy of the published messages!");
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            DeleteQueue(InputQueueName);
            DeleteQueue(AuditQueueName);
        }

        static void DeleteQueue(string queueName)
        {
            if (!MsmqUtil.QueueExists(queueName)) return;

            MsmqUtil.Delete(queueName);
        }

        public string GetEndpointFor(Type messageType)
        {
            return InputQueueName;
        }
    }
}