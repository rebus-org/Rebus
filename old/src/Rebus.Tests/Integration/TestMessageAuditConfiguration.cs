using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Tests.Util;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Description("Verifies that message audit can be enable with XML configuration")]
    public class TestMessageAuditConfiguration : FixtureBase
    {
        static readonly string AppConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Integration",
            "Config",
            "Audit.App.Config");

        const string InputQueue = "test.audit.input";
        const string ErrorQueue = "test.audit.error";
        const string AuditQueue = "test.audit.audit";

        protected override void DoSetUp()
        {
            MsmqUtil.EnsureMessageQueueExists(MsmqUtil.GetPath(AuditQueue));
        }

        [Test]
        public void ItWorks()
        {
            using (AppConfig.Change(AppConfigPath))
            using (var adapter = new BuiltinContainerAdapter())
            {
                var messageHandled = new ManualResetEvent(false);

                adapter.Handle<string>(s => messageHandled.Set());

                var bus = Configure.With(adapter)
                    .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                    .CreateBus()
                    .Start();

                bus.SendLocal("hello there!");

                messageHandled.WaitUntilSetOrDie(5.Seconds());
            }

            var auditedMessages = MsmqTestHelper
                .GetMessagesFrom(AuditQueue)
                .ToList();

            Assert.That(auditedMessages.Count, Is.EqualTo(1), "Expected to find exactly one copy of the sent message in the audit queue");
            Assert.That(auditedMessages.Single().Messages[0], Is.EqualTo("hello there!"));
        }

        protected override void DoTearDown()
        {
            var queuesToDelete = new[]
            {
                InputQueue,
                ErrorQueue,
                AuditQueue
            }.Where(MsmqUtil.QueueExists);

            foreach (var queue in queuesToDelete)
            {
                MsmqUtil.Delete(queue);
            }
        }
    }
}