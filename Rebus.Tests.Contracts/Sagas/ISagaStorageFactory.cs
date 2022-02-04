using Rebus.Sagas;

namespace Rebus.Tests.Contracts.Sagas;

public interface ISagaStorageFactory
{
    ISagaStorage GetSagaStorage();

    void CleanUp();
}