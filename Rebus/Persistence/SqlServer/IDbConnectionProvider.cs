using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public interface IDbConnectionProvider
    {
        Task<IDbConnection> GetConnection();
    }
}