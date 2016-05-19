using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus
{
    public interface IDataBusStorage
    {
        Task Save(string id, Stream source);
        Task Load(string id, Stream destination);
    }
}