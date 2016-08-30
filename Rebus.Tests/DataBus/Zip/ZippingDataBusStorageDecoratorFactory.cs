using System;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.Zip
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
            return new ZippingDataBusStorageDecorator(new InMemDataBusStorage(_inMemDataStore), DataCompressionMode.Always);
        }

        public void CleanUp()
        {
            Console.WriteLine($"The size is {_inMemDataStore.SizeBytes} bytes");
        }
    }
}