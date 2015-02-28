using System.Threading.Tasks;
using Rebus2.Sagas;

namespace Rebus2.Persistence.InMem
{
    public class InMemorySagaStorage : ISagaStorage
    {
        public async Task<ISagaData> Find(string propertyName, object propertyValue)
        {
            return null;
        }
    }
}