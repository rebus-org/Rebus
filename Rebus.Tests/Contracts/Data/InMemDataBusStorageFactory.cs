using Rebus.DataBus;

namespace Rebus.Tests.Contracts.Data
{
    public class InMemDataBusStorageFactory : IDataStorageFactory
    {
        readonly InMemDataStore _inMemDataStore = new InMemDataStore();

        public IDataBusStorage Create()
        {
            return new InMemDataBusStorage(_inMemDataStore);
        }
    }
}