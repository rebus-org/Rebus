using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Legacy;
using Rebus.Routing.TypeBased;
using Rebus.Subscriptions;
using Rebus.Tests.Extensions;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Integration.Legacy
{
    [TestFixture]
    public class TestLegacyPubSub : FixtureBase
    {
        string _oldEndpoint;
        string _newEndpoint;
        BuiltinHandlerActivator _activator;
        IBus _bus;
        Dictionary<string, HashSet<string>> _subscriptions;

        const string SubscriptionRequest = @"
{
  ""$type"": ""System.Object[], mscorlib"",
  ""$values"": [
    {
      ""$type"": ""Rebus.Messages.SubscriptionMessage, Rebus"",
      ""Type"": ""NewEndpoint.Messages.NewRequest, NewEndpoint.Messages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
      ""Action"": 0
    }
  ]
}
";
        
        const string ExpectedStringSubscriptionRequest = @"
{
  ""$type"": ""System.Object[], mscorlib"",
  ""$values"": [
    {
      ""$type"": ""Rebus.Messages.SubscriptionMessage, Rebus"",
      ""Type"": ""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"",
      ""Action"": 0
    }
  ]
}
";


        protected override void SetUp()
        {
            _newEndpoint = TestConfig.QueueName("newendpoint");
            _oldEndpoint = TestConfig.QueueName("oldendpoint");

            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(_newEndpoint));
            MsmqUtil.EnsureQueueExists(MsmqUtil.GetPath(_oldEndpoint));

            _activator = Using(new BuiltinHandlerActivator());

            _subscriptions = new Dictionary<string, HashSet<string>>();

            _bus = Configure.With(_activator)
                .Transport(t => t.UseMsmq(_newEndpoint))
                .Subscriptions(s => s.Decorate(c => new SubDec(c.Get<ISubscriptionStorage>(), _subscriptions)))
                .Routing(m => m.TypeBased().Map<string>(_oldEndpoint))
                .Options(o =>
                {
                    o.EnableLegacyCompatibility();
                    o.LogPipeline(false);
                })
                .Start();
        }

        [Test]
        public async Task CanHandleSubscriptionRequest()
        {
            var messageId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
                {
                    {"rebus-return-address", _oldEndpoint},
                    {"rebus-msg-id", messageId},
                    {"rebus-content-type", "text/json"},
                    {"rebus-encoding", "utf-7"}
                };

            using (var queue = new MessageQueue(MsmqUtil.GetFullPath(_newEndpoint)))
            {
                queue.SendLegacyRebusMessage(SubscriptionRequest, headers);
            }

            await Task.Delay(1000);

            var topic = "NewEndpoint.Messages.NewRequest, NewEndpoint.Messages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            Assert.That(_subscriptions.Count, Is.EqualTo(1));
            Assert.That(_subscriptions[topic].Count, Is.EqualTo(1));
            Assert.That(_subscriptions[topic].First(), Is.EqualTo(_oldEndpoint));
        }

        [Test]
        public async Task CanSubcribeWithOldSchoolSubscriptionRequest()
        {
            await _bus.Subscribe<string>();

            await Task.Delay(500);

            using (var queue = new MessageQueue(MsmqUtil.GetFullPath(_oldEndpoint)))
            {
                var message = queue.GetNextMessage();
                var headers = message.DeserializeHeaders();

                using (var streamReader = new StreamReader(message.BodyStream, Encoding.UTF7))
                {
                    var jsonText = streamReader.ReadToEnd();

                    Assert.That(jsonText.ToNormalizedJson(), Is.EqualTo(ExpectedStringSubscriptionRequest.ToNormalizedJson()));
                }

            }
        }

        class SubDec : ISubscriptionStorage
        {
            readonly ISubscriptionStorage _subscriptionStorage;
            readonly Dictionary<string, HashSet<string>> _currentSubscriptions;

            public SubDec(ISubscriptionStorage subscriptionStorage, Dictionary<string, HashSet<string>> currentSubscriptions)
            {
                _subscriptionStorage = subscriptionStorage;
                _currentSubscriptions = currentSubscriptions;
            }

            public async Task<string[]> GetSubscriberAddresses(string topic)
            {
                return await _subscriptionStorage.GetSubscriberAddresses(topic);
            }

            public async Task RegisterSubscriber(string topic, string subscriberAddress)
            {
                await _subscriptionStorage.RegisterSubscriber(topic, subscriberAddress);

                _currentSubscriptions
                    .GetOrAdd(topic, () => new HashSet<string>())
                    .Add(subscriberAddress);
            }

            public async Task UnregisterSubscriber(string topic, string subscriberAddress)
            {
                await _subscriptionStorage.UnregisterSubscriber(topic, subscriberAddress);

                _currentSubscriptions
                    .GetOrAdd(topic, () => new HashSet<string>())
                    .Remove(subscriberAddress);
            }

            public bool IsCentralized
            {
                get { return _subscriptionStorage.IsCentralized; }
            }
        }

        protected override void TearDown()
        {
            MsmqUtil.Delete(_newEndpoint);
            MsmqUtil.Delete(_oldEndpoint);
        }

    }
}