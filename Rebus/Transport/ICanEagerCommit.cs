using System.Threading.Tasks;

namespace Rebus.Transport;

interface ICanEagerCommit
{
    Task Commit();
}