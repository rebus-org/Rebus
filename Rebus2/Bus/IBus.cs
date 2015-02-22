using System;
using System.Threading.Tasks;

namespace Rebus2.Bus
{
    public interface IBus : IDisposable
    {
        Task Send(object commandMessage);
        Task Reply(object replyMessage);
        
        Task Publish(string topic, object eventMessage);
        Task Subscribe(string topic);
        Task Unsubscribe(string topic);
    }
}