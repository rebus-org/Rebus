using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Tests.Transports.Rabbit;
using Shouldly;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Integration
{
    [TestFixture, Description("Since publishing works differently when using a multicast-capable transport, we need to ensure that multicast is covered as well")]
    public class TestMessageAuditMulticast : FixtureBase
    {
        const string InputQueueName = "test.audit.input";
        const string AuditQueueName = "test.audit.audit";
        List<IDisposable> disposables;
        BuiltinContainerAdapter adapter;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();
            disposables = new List<IDisposable> { adapter };

            Configure.With(adapter)
                .Transport(t => t.UseRabbitMq(RabbitMqFixtureBase.ConnectionString, InputQueueName, "error"))
                .Behavior(b => b.EnableMessageAudit(AuditQueueName))
                .CreateBus()
                .Start(1);

            RabbitMqFixtureBase.DeleteQueue(AuditQueueName);
        }

        static string InputQueueAddress
        {
            get { return InputQueueName; }
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
            var message = GetMessageFrom(AuditQueueName);

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

            // act
            adapter.Bus.Publish("yo!");

            // assert
            var message = GetMessageFrom(AuditQueueName);

            message.ShouldNotBe(null);

            var logicalMessages = message.Messages;
            var headers = message.Headers;

            logicalMessages.Length.ShouldBe(1);
            logicalMessages[0].ShouldBe("yo!");

            headers.ShouldContainKeyAndValue(Headers.AuditReason, Headers.AuditReasons.Published);
            headers.ShouldContainKeyAndValue(Headers.AuditMessageCopyTime, fakeTime.ToString("u"));
            headers.ShouldContainKeyAndValue(Headers.AuditSourceQueue, InputQueueAddress);
        }

        protected override void DoTearDown()
        {
            disposables.ForEach(d => d.Dispose());
        }

        Message GetMessageFrom(string queueName)
        {
            var timer = Stopwatch.StartNew();
            using (var queue = new RabbitMqMessageQueue(RabbitMqFixtureBase.ConnectionString, queueName))
            {
                ReceivedTransportMessage receivedTransportMessage = null;

                var timeout = 2.Seconds();
                do
                {
                    receivedTransportMessage = queue.ReceiveMessage(new NoTransaction());
                } while (receivedTransportMessage == null && timer.Elapsed < timeout);

                if (receivedTransportMessage == null)
                {
                    throw new TimeoutException(string.Format("No message was received within {0} timeout", timeout));
                }

                var serializer = new JsonMessageSerializer();

                return serializer.Deserialize(receivedTransportMessage);
            }
        }
    }
}