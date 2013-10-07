using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Transports.Azure
{
    [TestFixture, Description("Allow for Rebus' Azure Service Bus to detect the special development storage connection string and use MSMQ to emulate the Azure transport")]
    public class TestDevelopmentStorageTransport : FixtureBase
    {
        const string AzureReceiverInputQueueName = "test.azure.receiver";
        const string MsmqSenderInputQueueName = "test.azure.sender";

        readonly List<IDisposable> disposables = new List<IDisposable>();
        BuiltinContainerAdapter azureAdapter;
        BuiltinContainerAdapter msmqAdapter;
        BuiltinContainerAdapter oneWayAzureAdapter;

        protected override void DoSetUp()
        {
            azureAdapter = NewAdapter();
            Configure.With(azureAdapter)
                     .Transport(t => t.UseAzureServiceBus("UseDevelopmentStorage=true", AzureReceiverInputQueueName, "error"))
                     .CreateBus()
                     .Start();

            msmqAdapter = NewAdapter();
            Configure.With(msmqAdapter)
                     .Transport(t => t.UseMsmq(MsmqSenderInputQueueName, "error"))
                     .CreateBus()
                     .Start();

            oneWayAzureAdapter = NewAdapter();
            Configure.With(oneWayAzureAdapter)
                     .Transport(t => t.UseAzureServiceBusInOneWayClientMode("UseDevelopmentStorage=True"))
                     .CreateBus()
                     .Start();
        }

        protected override void DoTearDown()
        {
            disposables.ForEach(d => d.Dispose());

            MsmqUtil.Delete(AzureReceiverInputQueueName);
            MsmqUtil.Delete(MsmqSenderInputQueueName);
        }

        BuiltinContainerAdapter NewAdapter()
        {
            var adapter = new BuiltinContainerAdapter();
            disposables.Add(adapter);
            return adapter;
        }

        [Test]
        public void AzureServiceBusIsJustEmulatingItsThingByActuallyUsingMsmqUnderneath()
        {
            var resetEvent = new ManualResetEvent(false);

            azureAdapter.Handle<string>(message => msmqAdapter.Bus.Reply(string.Format("I got your message: {0}", message)));
            msmqAdapter.Handle<string>(reply =>
                {
                    if (reply == "I got your message: H3LL0!!")
                    {
                        resetEvent.Set();
                    }
                });

            msmqAdapter.Bus.Advanced.Routing.Send(AzureReceiverInputQueueName, "H3LL0!!");

            var timeout = 2.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive reply within {0} timeout", timeout);
        }

        [Test]
        public void AzureServiceBusIsJustEmulatingItsThingByActuallyUsingMsmqUnderneathAlsoWithOneWayClient()
        {
            var resetEvent = new ManualResetEvent(false);

            azureAdapter.Handle<string>(message =>
                {
                    if (message == "H3LL0!!")
                    {
                        resetEvent.Set();
                    }
                });
            
            oneWayAzureAdapter.Bus.Advanced.Routing.Send(AzureReceiverInputQueueName, "H3LL0!!");

            var timeout = 2.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive reply within {0} timeout", timeout);
        }
    }
}