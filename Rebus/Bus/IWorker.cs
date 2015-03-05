using System;

namespace Rebus.Bus
{
    public interface IWorker : IDisposable
    {
        void Stop();
    }
}