using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.AzureStorage.Databus;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.DataBus;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.AzureStorage.Tests.DataBus
{
    public class AzureStorageDataBusStorageFactory : AzureStorageFactoryBase, IDataBusStorageFactory
    {
        public IDataBusStorage Create()
        {
            var sub = new AzureStorageDataBusStorage(StorageAccount, $"DataTest{DateTime.Now:yyyyMMddHHmmss}");

            return sub;
        }
    }
}
