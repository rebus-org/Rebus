using System;

namespace Rebus2.Bus
{
    public interface IWorker : IDisposable
    {
        void Stop();
    }
}