using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    public class AzureStorageSagaStorageBasicLoadAndSaveAndFindOperations :
        BasicLoadAndSaveAndFindOperations<AzureStorageSagaStorageFactory>
    {

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            //AzureStorageSagaStorageFactory.DropAndRecreateObjects();
        }
    }
}