using System;

namespace Rebus.Bus
{
    /// <summary>
    /// Implement this and install an instance in order to hook into Rebus' unit of work
    /// </summary>
    public interface IUnitOfWorkManager
    {
        IUnitOfWork Create();
    }

    public interface IUnitOfWork : IDisposable
    {
        void Commit();
        void Abort();
    }
}