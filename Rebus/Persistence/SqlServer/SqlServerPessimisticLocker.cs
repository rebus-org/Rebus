using System;
using System.Data;
using System.Threading.Tasks;
using Rebus.Sagas.Locking;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerPessimisticLocker : IPessimisticLocker
    {
        readonly IDbConnectionProvider _connectionProvider;

        public SqlServerPessimisticLocker(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<bool> TryAcquire(string lockId)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    if (command.Transaction == null)
                    {
                        throw new InvalidOperationException("SqlServerPessimisticLocker attempted to acquire a lock on a non-transactional SQL connection... please be sure that the connection supplied for the locker is transactional, because that is required by the [sp_getapplock] and [sp_releaseapplock] stored procedures");
                    }

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "sp_getapplock";

                    command.Parameters.Add("Resource", SqlDbType.NVarChar).Value = lockId;
                    command.Parameters.Add("LockMode", SqlDbType.NVarChar).Value = "Exclusive";
                    command.Parameters.Add("LockOwner", SqlDbType.NVarChar).Value = "Transaction";

                    var result = command.Parameters.Add("ReturnValue", SqlDbType.Int);
                    result.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync();

                    return (int) result.Value >= 0;
                }
            }
        }

        public async Task Release(string lockId)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    if (command.Transaction == null)
                    {
                        throw new InvalidOperationException("SqlServerPessimisticLocker attempted to acquire a lock on a non-transactional SQL connection... please be sure that the connection supplied for the locker is transactional, because that is required by the [sp_getapplock] and [sp_releaseapplock] stored procedures");
                    }

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "sp_releaseapplock";

                    command.Parameters.Add("Resource", SqlDbType.NVarChar).Value = lockId;

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}