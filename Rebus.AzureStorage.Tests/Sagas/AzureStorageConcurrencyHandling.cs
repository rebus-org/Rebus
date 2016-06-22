using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    public class AzureStorageConcurrencyHandling : ConcurrencyHandling<AzureStorageSagaStorageFactory> { }
}