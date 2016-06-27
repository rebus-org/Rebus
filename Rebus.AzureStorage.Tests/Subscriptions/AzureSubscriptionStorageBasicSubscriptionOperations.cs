using NUnit.Framework;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.AzureStorage.Tests.Subscriptions
{
    public class AzureSubscriptionStorageBasicSubscriptionOperations :
        BasicSubscriptionOperations<AzureStorageSubscriptionStorageFactory>
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            AzureStorageSubscriptionStorageFactory.CreateTables();
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            AzureStorageSubscriptionStorageFactory.DropTables();
        }
    }
}