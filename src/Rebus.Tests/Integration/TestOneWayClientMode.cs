using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Tests.Configuration;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestOneWayClientMode : RebusBusMsmqIntegrationTestBase
    {
        const string ReceiverInputQueueName = "test.oneWayClientMode.receiver";

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(string))
            {
                return ReceiverInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }

        [Test]
        public void ThrowsWhenSubscribing()
        {
            // arrange
            using (var builtinContainerAdapter = new BuiltinContainerAdapter())
            {
                var bus = Configure.With(builtinContainerAdapter)
                    .Transport(t => t.UseMsmqInOneWayClientMode())
                    .DetermineEndpoints(d => d.Use(this))
                    .CreateBus()
                    .Start();


                // act
                var exception = Assert.Throws<InvalidOperationException>(bus.Subscribe<string>);

                // assert
                exception.Message.ShouldContain("one-way client mode");
            }
        }

        [Test]
        public void ThrowsWhenDoingSendLocal()
        {
            // arrange
            using (var builtinContainerAdapter = new BuiltinContainerAdapter())
            {
                var bus = Configure.With(builtinContainerAdapter)
                    .Transport(t => t.UseMsmqInOneWayClientMode())
                    .DetermineEndpoints(d => d.Use(this))
                    .CreateBus()
                    .Start();


                // act
                var exception = Assert.Throws<InvalidOperationException>(() => bus.SendLocal("w00t this should throw!!!"));

                // assert
                exception.Message.ShouldContain("one-way client mode");
            }
        }

        [Test]
        public void CanSendAutomaticallyRoutedMessages()
        {
            var resetEvent = new ManualResetEvent(false);

            CreateBus(ReceiverInputQueueName, new HandlerActivatorForTesting()
                                             .Handle<string>(str => resetEvent.Set()))
                .Start();

            var adapter = new TestConfigurationApi.TestContainerAdapter();

            var bus = Configure.With(adapter)
                .Transport(t => t.UseMsmqInOneWayClientMode())
                .DetermineEndpoints(d => d.Use(this))
                .CreateBus()
                .Start();

            EnsureProperDisposal(bus);

            bus.Send("w00t!!!!!!!!!!!1");

            var timeout = 3.Seconds();

            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not receive message within timeout of {0}", timeout);
            }
        }

        [Test]
        public void CanSendExplicitlyRoutedMessages()
        {
            const string receiverQueueName = "test.oneWayClientMode.receiver";
            var resetEvent = new ManualResetEvent(false);

            CreateBus(receiverQueueName, new HandlerActivatorForTesting()
                                             .Handle<string>(str => resetEvent.Set()))
                .Start();

            var adapter = new TestConfigurationApi.TestContainerAdapter();

            var bus = Configure.With(adapter)
                .Transport(t => t.UseMsmqInOneWayClientMode())
                .CreateBus()
                .Start();

            EnsureProperDisposal(bus);

            var advancedBus = (IAdvancedBus)bus;

            advancedBus.Routing.Send(receiverQueueName, "w00t!!!!!!!!!!!1");

            var timeout = 3.Seconds();

            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not receive message within timeout of {0}", timeout);
            }
        }
    }
}