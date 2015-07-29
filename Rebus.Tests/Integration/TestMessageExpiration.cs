using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(Categories.Msmq)]
    public class TestMessageExpiration : FixtureBase
    {
        readonly string _inputQueueName = TestConfig.QueueName("expiration");

        protected override void SetUp()
        {
            MsmqUtil.PurgeQueue(_inputQueueName);
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_inputQueueName);
        }

        [Test]
        public async Task ItWorksWithMsmq()
        {
            var receivedMessages = new ConcurrentQueue<string>();

            var headers = new Dictionary<string, string>
            {
                {Headers.TimeToBeReceived, "00:00:04"}
            };

            using (var sendOnlyBus = GetBus(receivedMessages, 0))
            {
                await sendOnlyBus.SendLocal("hej med dig (denne besked bliver ikke modtaget)", headers);
                
                await Task.Delay(5000);
            }

            using (var receiverBus = GetBus(receivedMessages, 1))
            {
                await receiverBus.SendLocal("hej med dig (det gør den her)", headers);

                await Task.Delay(2000);
            }

            Assert.That(receivedMessages.Count, Is.EqualTo(1), "Expected only one message - got: {0}", string.Join(", ", receivedMessages));
            Assert.That(receivedMessages.Single(), Is.EqualTo("hej med dig (det gør den her)"));
        }

        IBus GetBus(ConcurrentQueue<string> receivedMessages, int numberOfWorkers)
        {
            var activator = new BuiltinHandlerActivator();
            activator.Handle<string>(async str => receivedMessages.Enqueue(str));

            var bus = Configure.With(activator)
                .Transport(t => t.UseMsmq(_inputQueueName))
                .Options(o => o.SetNumberOfWorkers(numberOfWorkers))
                .Start();

            return bus;
        }

        /*
         *             var expressDelivery = transportMessage.Headers.ContainsKey(Headers.Express);

            var hasTimeout = transportMessage.Headers.ContainsKey(Headers.TimeToBeReceived);

            // make undelivered messages go to the dead letter queue if they could disappear from the queue anyway
            message.UseDeadLetterQueue = !(expressDelivery || hasTimeout);
            message.Recoverable = !expressDelivery;

            if (hasTimeout)
            {
                var timeToBeReceivedStr = (string)transportMessage.Headers[Headers.TimeToBeReceived];
                message.TimeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            }
*/
    }
}