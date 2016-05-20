using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using IDbConnection = Rebus.Persistence.SqlServer.IDbConnection;

namespace Rebus.DataBus.SqlServer
{
    public class SqlServerDataBusStorage : IDataBusStorage, IInitializable
    {
        readonly IDbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly bool _ensureTableIsCreated;
        readonly ILog _log;

        public SqlServerDataBusStorage(IDbConnectionProvider connectionProvider, string tableName, bool ensureTableIsCreated, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _connectionProvider = connectionProvider;
            _tableName = tableName;
            _ensureTableIsCreated = ensureTableIsCreated;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

        public void Initialize()
        {
            if (!_ensureTableIsCreated) return;

            _log.Info("Creating data bus table [{0}]", _tableName);

            EnsureTableIsCreated().Wait();
        }

        async Task EnsureTableIsCreated()
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                //if (connection.GetTableNames().Contains(_tableName, StringComparer.CurrentCultureIgnoreCase))
                //    return;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"

CREATE TABLE [{_tableName}] (
    [Id] VARCHAR(200),
    [Data] VARBINARY(MAX)
);

";
                    const int tableAlreadyExists = 2714;


                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException exception) when (exception.Number == tableAlreadyExists)
                    {
                        // table already exists - just quit now
                        return;
                    }
                }

                await connection.Complete();
            }
        }

        public async Task Save(string id, Stream source)
        {
            try
            {
                using (var connection = await _connectionProvider.GetConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO [{_tableName}] ([Id],[Data]) VALUES (@id,@data)";
                        command.Parameters.Add("id", SqlDbType.VarChar, 200).Value = id;
                        command.Parameters.Add("data", SqlDbType.VarBinary).Value = source;

                        await command.ExecuteNonQueryAsync();
                    }

                    await connection.Complete();
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not save data with ID {id}");
            }
        }

        public Stream Read(string id)
        {
            try
            {
                using (var connection = DispatchResult(() => _connectionProvider.GetConnection()))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT TOP 1 [Data] FROM [{_tableName}] WITH (NOLOCK) WHERE [Id] = @id";
                        command.Parameters.Add("id", SqlDbType.VarChar, 200).Value = id;

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var stream = reader.GetStream(reader.GetOrdinal("data"));

                                return stream;
                            }

                            throw new ArgumentException($"Row with ID {id} not found");
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not load data with ID {id}");
            }
        }

        //static void Dispatch(Func<Task> action)
        //{
        //    var done = new ManualResetEvent(false);
        //    ThreadPool.QueueUserWorkItem(_ =>
        //    {
        //        action().ContinueWith(task =>
        //        {
        //            done.Set();
        //        });
        //    });
        //    if (!done.WaitOne(TimeSpan.FromSeconds(5)))
        //    {
        //        throw new RebusApplicationException($"Did not get result from background thread within 5 s timeout");
        //    }
        //}

        static TResult DispatchResult<TResult>(Func<Task<TResult>> function)
        {
            var result = default(TResult);
            var done = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                function().ContinueWith(task =>
                {
                    result = task.Result;
                    done.Set();
                });
            });
            if (!done.WaitOne(TimeSpan.FromSeconds(5)))
            {
                throw new RebusApplicationException($"Did not get result from background thread within 5 s timeout");
            }
            return result;
        }
    }
}