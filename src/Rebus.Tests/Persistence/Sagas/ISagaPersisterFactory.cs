using System;

namespace Rebus.Tests.Persistence.Sagas
{
    public interface ISagaPersisterFactory : IDisposable
    {
        IStoreSagaData CreatePersister();
    }
}