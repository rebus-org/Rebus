using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Tests.Transports.Rabbit;
using Rebus.Transports.Msmq;
using Rebus.RabbitMQ;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class DoesNotLeakChannelsWhenCombiningRabbitWithMsmq : RabbitMqFixtureBase
    {
        const string InputQueueName = "test.rabbitleak.input";
        BuiltinContainerAdapter msmqBus;
        BuiltinContainerAdapter rabbitBus;
        readonly List<IDisposable> stuffToDispose = new List<IDisposable>();
        
        protected override void DoSetUp()
        {
            msmqBus = new BuiltinContainerAdapter();
            stuffToDispose.Add(msmqBus);

            Configure.With(msmqBus)
                     .Transport(t => t.UseMsmq(InputQueueName, "error"))
                     .CreateBus()
                     .Start(1);

            rabbitBus = new BuiltinContainerAdapter();
            stuffToDispose.Add(rabbitBus);

            Configure.With(rabbitBus)
                     .Transport(t => t.UseRabbitMqInOneWayMode(ConnectionString)
                                      .ManageSubscriptions())
                     .CreateBus()
                     .Start(1);
        }

        protected override void DoTearDown()
        {
            stuffToDispose.ForEach(d => d.Dispose());
        }

        [Test, Ignore("This test can't really assert that no channels are leaked, which is why it should only be run manually (and then you have 30 seconds to see the leaked channels in the Rabbit console)")]
        public void DoesNotLeakChannels()
        {
            // let MSMQ bus "bridge" between MSMQ and Rabbit by publishing all received strings
            var receivedMessages = 0;
            var resetEvent = new ManualResetEvent(false);
            msmqBus.Handle<string>(str =>
                {
                    rabbitBus.Bus.Publish(str);

                    receivedMessages++;

                    if (receivedMessages == 100)
                    {
                        resetEvent.Set();
                    }
                });

            var counter = 1;
            100.Times(() => msmqBus.Bus.SendLocal(string.Format("This is message {0}", counter++)));

            var timeout = 20.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.Not.False, "Did not receive all the messages withing {0} timeout");

            Thread.Sleep(30.Seconds());
        }
    }
}