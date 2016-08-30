using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageSagaStorageBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<AzureStorageSagaStorageFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStoragerSagaStorageConcurrencyHandling : ConcurrencyHandling<AzureStorageSagaStorageFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageSagaStorageSagaIntegrationTests : SagaIntegrationTests<AzureStorageSagaStorageFactory> { }
}