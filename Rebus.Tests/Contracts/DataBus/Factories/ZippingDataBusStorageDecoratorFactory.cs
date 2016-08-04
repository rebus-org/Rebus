using System;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.DataBus.InMem;

namespace Rebus.Tests.Contracts.DataBus.Factories
{
    /*
     * 
     * CanSaveAndLoadBiggerPieceOfData:
     
    initial:
        The size is 48889 bytes

    w. zip:
        The size is 22196 bytes
  
    */
    public class ZippingDataBusStorageDecoratorFactory : IDataBusStorageFactory
    {
        readonly InMemDataStore _inMemDataStore = new InMemDataStore();

        public IDataBusStorage Create()
        {
            return new ZippingDataBusStorageDecorator(new InMemDataBusStorage(_inMemDataStore), DataCompressionMode.AlwaysCompress);
        }

        public void CleanUp()
        {
            Console.WriteLine($"The size is {_inMemDataStore.SizeBytes} bytes");
        }
    }
}