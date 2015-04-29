using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public interface IDbConnection : IDisposable
    {
        SqlCommand CreateCommand();
        IEnumerable<string> GetTableNames();
        Task Complete();
    }
}