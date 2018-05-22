using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    public interface IHandleSagaExlusiveLock
    {
        Task<bool> AquireLockAsync(string key);
        Task<bool> ReleaseLockAsync(string key);

    }
}