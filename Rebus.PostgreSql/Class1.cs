using System.Threading.Tasks;
using Rebus.Subscriptions;

namespace Rebus.PostgreSql
{
    public class PostgreSqlSubscriptionStorage : ISubscriptionStorage
    {
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            throw new System.NotImplementedException();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool IsCentralized
        {
            get; private set;
        }
    }
}
