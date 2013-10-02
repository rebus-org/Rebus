using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestRebusBusWithAzureServiceBusAndOneWayClient : FixtureBase
    {
        const string RecipientInputQueueName = "test.oneway.recipient";
        readonly List<IDisposable> stuffToDispose = new List<IDisposable>();
        BuiltinContainerAdapter recipientAdapter;
        BuiltinContainerAdapter senderAdapter;

        protected override void DoSetUp()
        {
            recipientAdapter = new BuiltinContainerAdapter();
            stuffToDispose.Add(recipientAdapter);

            senderAdapter = new BuiltinContainerAdapter();
            stuffToDispose.Add(senderAdapter);

            Configure.With(recipientAdapter)
                     .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, RecipientInputQueueName, "error"))
                     .CreateBus()
                     .Start();

            Configure.With(senderAdapter)
                     .Transport(t => t.UseAzureServiceBusInOneWayClientMode(AzureServiceBusMessageQueueFactory.ConnectionString))
                     .CreateBus()
                     .Start();
        }

        protected override void DoTearDown()
        {
            stuffToDispose.ForEach(d => d.Dispose());
        }

        [Test]
        public void YesItWorks()
        {
            var resetEvent = new ManualResetEvent(false);
            recipientAdapter.Handle<string>(str =>
                {
                    if (str == "hullo!")
                    {
                        resetEvent.Set();
                    }
                });

            senderAdapter.Bus.Advanced.Routing.Send(RecipientInputQueueName, "hullo!");

            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True,
                        "Does not look like we received the message withing {0} timeout", timeout);
        }
    }
}