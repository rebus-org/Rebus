using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.AzureStorage.Tests.Sagas
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageSagaStorageBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<AzureStorageSagaStorageFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStoragerSagaStorageConcurrencyHandling : ConcurrencyHandling<SqlServerSagaStorageFactory> { }

    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageSagaStorageSagaIntegrationTests : SagaIntegrationTests<SqlServerSagaStorageFactory> { }
}