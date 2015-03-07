using System;

namespace Rebus.Bus
{
    public interface IWorker : IDisposable
    {
        string Name { get; }
        void Stop();
    }
}