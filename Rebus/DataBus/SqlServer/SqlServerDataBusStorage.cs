using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Serialization;
using Rebus.Time;

namespace Rebus.DataBus.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that uses SQL Server to store data
    /// </summary>
    public class SqlServerDataBusStorage : IDataBusStorage, IInitializable
    {
        static readonly Encoding TextEncoding = Encoding.UTF8;
        readonly DictionarySerializer _dictionarySerializer = new DictionarySerializer();
        readonly IDbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly bool _ensureTableIsCreated;
        readonly ILog _log;

        /// <summary>
        /// Creates the data storage
        /// </summary>
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

        /// <summary>
        /// Initializes the SQL Server data storage.
        /// Will create the data table, unless this has been explicitly turned off when configuring the data storage
        /// </summary>
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
                if (connection.GetTableNames().Contains(_tableName, StringComparer.CurrentCultureIgnoreCase))
                    return;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"

CREATE TABLE [{_tableName}] (
    [Id] VARCHAR(200),
    [Meta] VARBINARY(MAX),
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

        /// <summary>
        /// Saves the data from the given source stream under the given ID
        /// </summary>
        public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
        {
            var metadataToWrite = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>())
            {
                [MetadataKeys.SaveTime] = RebusTime.Now.ToString("O")
            };

            try
            {
                using (var connection = await _connectionProvider.GetConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO [{_tableName}] ([Id], [Meta], [Data]) VALUES (@id, @meta, @data)";
                        command.Parameters.Add("id", SqlDbType.VarChar, 200).Value = id;
                        command.Parameters.Add("meta", SqlDbType.VarBinary).Value = TextEncoding.GetBytes(_dictionarySerializer.SerializeToString(metadataToWrite));
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

        /// <summary>
        /// Opens the data stored under the given ID for reading
        /// </summary>
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

        /// <summary>
        /// Loads the metadata stored with the given ID
        /// </summary>
        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            try
            {
                using (var connection = await _connectionProvider.GetConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT TOP 1 [Meta], DATALENGTH([Data]) AS 'Length' FROM [{_tableName}] WITH (NOLOCK) WHERE [Id] = @id";
                        command.Parameters.Add("id", SqlDbType.VarChar, 200).Value = id;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new ArgumentException($"Row with ID {id} not found");
                            }

                            var bytes = (byte[])reader["Meta"];
                            var length = (long) reader["Length"];
                            var jsonText = TextEncoding.GetString(bytes);
                            var metadata = _dictionarySerializer.DeserializeFromString(jsonText);
                            metadata[MetadataKeys.Length] = length.ToString();
                            return metadata;
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
                throw new RebusApplicationException(exception, $"Could not load metadata for data with ID {id}");
            }
        }

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
                throw new RebusApplicationException("Did not get result from background thread within 5 s timeout");
            }
            return result;
        }
    }
}