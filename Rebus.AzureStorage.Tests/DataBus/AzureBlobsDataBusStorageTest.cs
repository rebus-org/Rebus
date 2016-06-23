using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AzureStorage.Tests.DataBus
{
    [TestFixture]
    public class AzureBlobsDataBusStorageTest : GeneralDataBusStorageTests<AzureBlobsDataBusStorageFactory> { }
}