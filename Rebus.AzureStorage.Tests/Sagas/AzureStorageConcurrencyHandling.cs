using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageConcurrencyHandling : ConcurrencyHandling<AzureStorageSagaStorageFactory> { }
}