using System;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts
{
    public interface ITimeoutStorageFactory : IDisposable
    {
        IStoreTimeouts CreateStore();
    }
}