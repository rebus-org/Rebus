using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Extensions;
#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    [TestFixture, Category("rabbitmq")]
    public class RabbitMqPubSubTest : FixtureBase
    {
        readonly string _publisherQueueName = TestConfig.QueueName("publisher");
        readonly string _subscriber1QueueName = TestConfig.QueueName("sub1");
        readonly string _subscriber2QueueName = TestConfig.QueueName("sub2");
        BuiltinHandlerActivator _publisher;

        protected override void SetUp()
        {
            RabbitMqTransportFactory.DeleteQueue(_publisherQueueName);
            RabbitMqTransportFactory.DeleteQueue(_subscriber1QueueName);
            RabbitMqTransportFactory.DeleteQueue(_subscriber2QueueName);

            _publisher = GetBus(_publisherQueueName);
        }

        [Test]
        public async Task ItWorks()
        {
            var sub1GotEvent = new ManualResetEvent(false);
            var sub2GotEvent = new ManualResetEvent(false);

            var sub1 = GetBus(_subscriber1QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub1GotEvent.Set();
                }
            });

            var sub2 = GetBus(_subscriber2QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub2GotEvent.Set();
                }
            });

            await sub1.Bus.Subscribe<string>();
            await sub2.Bus.Subscribe<string>();

            await _publisher.Bus.Publish("weehoo!!");

            sub1GotEvent.WaitOrDie(TimeSpan.FromSeconds(2));
            sub2GotEvent.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        BuiltinHandlerActivator GetBus(string queueName, Func<string, Task> handlerMethod = null)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            if (handlerMethod != null)
            {
                activator.Handle(handlerMethod);
            }

            Configure.With(activator)
                .Transport(t =>
                {
                    t.UseRabbitMq(RabbitMqTransportFactory.ConnectionString, queueName)
                        .AddClientProperties(new Dictionary<string, string>
                        {
                            {"description", "pub-sub test in RabbitMqPubSubTest.cs"}
                        });
                })
                .Start();

            return activator;
        }
    }
}