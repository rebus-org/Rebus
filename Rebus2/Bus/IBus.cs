using System;
using System.Threading.Tasks;

namespace Rebus2.Bus
{
    public interface IBus : IDisposable
    {
        Task Send(object message);
        Task Reply(object message);
    }
}