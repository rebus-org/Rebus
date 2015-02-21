using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Routing
{
    public interface ISubscriptionStorage
    {
        Task<IEnumerable<string>> GetSubscriberAddresses(string topic);

        Task RegisterSubscriber(string topic, string subscriberAddress);
        
        Task UnregisterSubscriber(string topic, string subscriberAddress);
    }
}