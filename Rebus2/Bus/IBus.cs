using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Bus
{
    public interface IBus : IDisposable
    {
        Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null);
        
        Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null);
        
        Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null);

        Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null);
        
        Task Subscribe(string topic);
        Task Unsubscribe(string topic);
    }
}