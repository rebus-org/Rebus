using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.InMem
{
    public class InMemDataBusStorageFactory : IDataBusStorageFactory
    {
        readonly InMemDataStore _inMemDataStore = new InMemDataStore();

        public IDataBusStorage Create()
        {
            return new InMemDataBusStorage(_inMemDataStore);
        }

        public void CleanUp()
        {
            
        }
    }
}