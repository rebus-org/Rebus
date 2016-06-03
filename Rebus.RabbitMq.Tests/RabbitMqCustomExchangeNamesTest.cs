using System;
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
    [TestFixture]
    public class RabbitMqCustomExchangeNamesTest : FixtureBase
    {
        [Test]
        public async Task CanUseCustomExchangeName()
        {
            const string connectionString = RabbitMqTransportFactory.ConnectionString;

            const string customDirectExchangeName = "Dingo";
            const string customTopicExchangeName = "Topico";

            RabbitMqTransportFactory.DeleteExchange(RabbitMqOptionsBuilder.DefaultDirectExchangeName);
            RabbitMqTransportFactory.DeleteExchange(RabbitMqOptionsBuilder.DefaultTopicExchangeName);
            RabbitMqTransportFactory.DeleteExchange(customDirectExchangeName);
            RabbitMqTransportFactory.DeleteExchange(customTopicExchangeName);

            using (var activator = new BuiltinHandlerActivator())
            {
                var gotString = new ManualResetEvent(false);
                activator.Handle<string>(async str => gotString.Set());

                Configure.With(activator)
                    .Transport(t =>
                    {
                        var queueName = TestConfig.QueueName("custom-exchange");

                        t.UseRabbitMq(connectionString, queueName)
                            .ExchangeNames(directExchangeName: customDirectExchangeName, topicExchangeName: customTopicExchangeName);
                    })
                    .Start();

                await activator.Bus.SendLocal("hej");

                gotString.WaitOrDie(TimeSpan.FromSeconds(3));
            }

            Assert.That(RabbitMqTransportFactory.ExchangeExists(RabbitMqOptionsBuilder.DefaultDirectExchangeName), Is.False);
            Assert.That(RabbitMqTransportFactory.ExchangeExists(RabbitMqOptionsBuilder.DefaultTopicExchangeName), Is.False);
            Assert.That(RabbitMqTransportFactory.ExchangeExists(customDirectExchangeName), Is.False);
            Assert.That(RabbitMqTransportFactory.ExchangeExists(customTopicExchangeName), Is.False);
        }
    }
}