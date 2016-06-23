using Rebus.DataBus;

namespace Rebus.Tests.Contracts.DataBus
{
    public interface IDataBusStorageFactory
    {
        IDataBusStorage Create();
        void CleanUp();
    }
}