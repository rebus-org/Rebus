using System.Threading.Tasks;

namespace Rebus2.Sagas
{
    public interface ISagaStorage
    {
        Task<ISagaData> Find(string propertyName, object propertyValue);
    }
}