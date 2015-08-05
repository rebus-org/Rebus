using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="ITimeoutManager"/> that uses SQL Server to store messages until it's time to deliver them.
    /// </summary>
    public class SqlServerTimeoutManager : ITimeoutManager
    {
        static ILog _log;

        static SqlServerTimeoutManager()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly DbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly JsonSerializerSettings _headerSerializationSettings = new JsonSerializerSettings();

        /// <summary>
        /// Constructs the timeout manager, using the specified connection provider and table to store the messages until they're due.
        /// </summary>
        public SqlServerTimeoutManager(DbConnectionProvider connectionProvider, string tableName)
        {
            _connectionProvider = connectionProvider;
            _tableName = tableName;
        }

        /// <summary>
        /// Creates the due messages table if necessary
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionProvider.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(_tableName, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                _log.Info("Table '{0}' does not exist - it will be created now", _tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}](
    [id] [int] IDENTITY(1,1) NOT NULL,
	[due_time] [datetime2](7) NOT NULL,
	[headers] [nvarchar](MAX) NOT NULL,
	[body] [varbinary](MAX) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY NONCLUSTERED 
    (
	    [id] ASC
    )
)
", _tableName);
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE CLUSTERED INDEX [IX_{0}_DueTime] ON [dbo].[{0}]
(
	[due_time] ASC
)
", _tableName);

                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }

        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var headersString = JsonConvert.SerializeObject(headers, _headerSerializationSettings);

            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"INSERT INTO [{0}] ([due_time], [headers], [body]) VALUES (@due_time, @headers, @body)", _tableName);

                    command.Parameters.Add("due_time", SqlDbType.DateTime2).Value = approximateDueTime.UtcDateTime;
                    command.Parameters.Add("headers", SqlDbType.NVarChar).Value = headersString;
                    command.Parameters.Add("body", SqlDbType.VarBinary).Value = body;

                    await command.ExecuteNonQueryAsync();
                }

                await connection.Complete();
            }
        }

        public async Task<DueMessagesResult> GetDueMessages()
        {
            var connection = await _connectionProvider.GetConnection();
            try
            {
                var dueMessages = new List<DueMessage>();

                const int maxDueTimeouts = 1000;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            @"
SELECT 
    [id],
    [headers],
    [body]
FROM [{0}] WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE [due_time] <= @current_time 
ORDER BY [due_time] ASC
",
                            _tableName);

                    command.Parameters.AddWithValue("current_time", RebusTime.Now);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = (int)reader["id"];
                            var headersString = (string)reader["headers"];
                            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersString, _headerSerializationSettings);
                            var body = (byte[])reader["body"];

                            var sqlTimeout = new DueMessage(headers, body, () =>
                            {
                                using (var deleteCommand = connection.CreateCommand())
                                {
                                    deleteCommand.CommandText = string.Format("DELETE FROM [{0}] WHERE [id] = @id", _tableName);
                                    deleteCommand.Parameters.Add("id", SqlDbType.Int).Value = id;
                                    deleteCommand.ExecuteNonQuery();
                                }
                            });

                            dueMessages.Add(sqlTimeout);

                            if (dueMessages.Count >= maxDueTimeouts) break;
                        }
                    }

                    return new DueMessagesResult(dueMessages, async () =>
                    {
                        using (connection)
                        {
                            await connection.Complete();
                        }
                    });
                }
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }
    }
}