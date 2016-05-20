using Rebus.DataBus;

namespace Rebus.Tests.Contracts.Data
{
    public interface IDataStorageFactory
    {
        IDataBusStorage Create();
    }
}