using Rebus2.Sagas;

namespace Rebus.Tests.Contracts.Sagas
{
    public interface ISagaStorageFactory
    {
        ISagaStorage GetSagaStorage();

        void Cleanup();
    }
}