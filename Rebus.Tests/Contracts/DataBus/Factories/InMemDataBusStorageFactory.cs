using Rebus.DataBus;
using Rebus.DataBus.InMem;

namespace Rebus.Tests.Contracts.DataBus.Factories
{
    public class InMemDataBusStorageFactory : IDataBusStorageFactory
    {
        readonly InMemDataStore _inMemDataStore = new InMemDataStore();

        public IDataBusStorage Create()
        {
            return new InMemDataBusStorage(_inMemDataStore);
        }
    }
}