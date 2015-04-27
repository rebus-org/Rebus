using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public class DbConnectionProvider
    {
        readonly string _connectionString;

        public DbConnectionProvider(string connectionStringOrConnectionStringName)
        {
            var connectionString = GetConnectionString(connectionStringOrConnectionStringName);

            _connectionString = EnsureMarsIsEnabled(connectionString);

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        string EnsureMarsIsEnabled(string connectionString)
        {
            var connectionStringParameters = connectionString.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(kvpString =>
                {
                    var tokens = kvpString.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    return new
                    {
                        Key = tokens[0],
                        Value = string.Join("=", tokens.Skip(1))
                    };
                })
                .ToDictionary(a => a.Key, a => a.Value, StringComparer.InvariantCultureIgnoreCase);

            if (!connectionStringParameters.ContainsKey("MultipleActiveResultSets"))
            {
                return connectionString + ";MultipleActiveResultSets=true";
            }

            return connectionString;
        }

        string GetConnectionString(string connectionStringOrConnectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringOrConnectionStringName];

            if (connectionStringSettings != null)
                return connectionStringSettings.ConnectionString;

            return connectionStringOrConnectionStringName;
        }

        public async Task<DbConnection> GetConnection()
        {
            var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            return new DbConnection(connection, connection.BeginTransaction(IsolationLevel), false);
        }

        public IsolationLevel IsolationLevel { get; set; }
    }
}