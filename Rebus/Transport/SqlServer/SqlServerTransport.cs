using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.SqlServer;
using Rebus.Threading;
using IDbConnection = Rebus.Persistence.SqlServer.IDbConnection;

namespace Rebus.Transport.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses SQL Server to do its thing
    /// </summary>
    public class SqlServerTransport : ITransport, IInitializable, IDisposable
    {
        readonly AsyncBottleneck _bottleneck = new AsyncBottleneck(20);

        /// <summary>
        /// Special message priority header that can be used with the <see cref="SqlServerTransport"/>. The value must be an <see cref="Int32"/>
        /// </summary>
        public const string MessagePriorityHeaderKey = "rbs2-msg-priority";

        /// <summary>
        /// Default interval that will be used for <see cref="ExpiredMessagesCleanupInterval"/> unless it is explicitly set to something else
        /// </summary>
        public static readonly TimeSpan DefaultExpiredMessagesCleanupInterval = TimeSpan.FromSeconds(20);

        static ILog _log;

        static SqlServerTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        const string CurrentConnectionKey = "sql-server-transport-current-connection";
        const int RecipientColumnSize = 200;

        readonly HeaderSerializer _headerSerializer = new HeaderSerializer();
        readonly IDbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly string _inputQueueName;

        readonly AsyncTask _expiredMessagesCleanupTask;
        bool _disposed;

        /// <summary>
        /// Constructs the transport with the given <see cref="IDbConnectionProvider"/>, using the specified <paramref name="tableName"/> to send/receive messages,
        /// querying for messages with recipient = <paramref name="inputQueueName"/>
        /// </summary>
        public SqlServerTransport(IDbConnectionProvider connectionProvider, string tableName, string inputQueueName)
        {
            _connectionProvider = connectionProvider;
            _tableName = tableName;
            _inputQueueName = inputQueueName;

            ExpiredMessagesCleanupInterval = DefaultExpiredMessagesCleanupInterval;

            _expiredMessagesCleanupTask = new AsyncTask("ExpiredMessagesCleanup", PerformExpiredMessagesCleanupCycle)
            {
                Interval = TimeSpan.FromMinutes(1)
            };
        }

        ~SqlServerTransport()
        {
            Dispose(false);
        }

        public void Initialize()
        {
            _expiredMessagesCleanupTask.Start();
        }

        /// <summary>
        /// Configures the interval between periodic deletion of expired messages. Defaults to <see cref="DefaultExpiredMessagesCleanupInterval"/>
        /// </summary>
        public TimeSpan ExpiredMessagesCleanupInterval { get; set; }

        async Task PerformExpiredMessagesCleanupCycle()
        {
            int results;
            var stopwatch = Stopwatch.StartNew();

            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format("DELETE FROM [{0}] WHERE [recipient] = @recipient AND [expiration] < getdate()", _tableName);
                    command.Parameters.Add("recipient", SqlDbType.NVarChar, RecipientColumnSize).Value = _inputQueueName;

                    results = await command.ExecuteNonQueryAsync();
                }

                await connection.Complete();
            }

            if (results > 0)
            {
                _log.Info("Performed expired messages cleanup in {0} - {1} expired messages with recipient {2} were deleted",
                    stopwatch.Elapsed, results, _inputQueueName);
            }
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var connection = await GetConnection(context);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"
INSERT INTO [{0}]
(
    [recipient],
    [headers],
    [body],
    [priority],
    [expiration]
)
VALUES
(
    @recipient,
    @headers,
    @body,
    @priority,
    dateadd(ss, @ttlseconds, getdate())
)",
                    _tableName);

                var priority = GetMessagePriority(message);

                command.Parameters.Add("recipient", SqlDbType.NVarChar, RecipientColumnSize).Value = destinationAddress;
                command.Parameters.Add("headers", SqlDbType.VarBinary).Value = _headerSerializer.Serialize(message.Headers);
                command.Parameters.Add("body", SqlDbType.VarBinary).Value = message.Body;
                command.Parameters.Add("priority", SqlDbType.Int).Value = priority;
                command.Parameters.Add("@ttlseconds", SqlDbType.Int).Value = GetTtlSeconds(message.Headers);

                await command.ExecuteNonQueryAsync();
            }
        }

        static int GetTtlSeconds(Dictionary<string, string> headers)
        {
            const int defaultTtlSecondsAbout60Years = int.MaxValue;

            if (!headers.ContainsKey(Headers.TimeToBeReceived))
                return defaultTtlSecondsAbout60Years;

            var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
            var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);

            return (int)timeToBeReceived.TotalSeconds;
        }

        class HeaderSerializer
        {
            static readonly Encoding DefaultEncoding = Encoding.UTF8;

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                return DefaultEncoding.GetBytes(JsonConvert.SerializeObject(headers));
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(DefaultEncoding.GetString(bytes));
            }
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            using (await _bottleneck.Enter())
            {
                var connection = await GetConnection(context);

                long? idOfMessageToDelete;
                TransportMessage receivedTransportMessage;

                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.CommandText =
                        string.Format(@"
SELECT TOP 1
    [id],
    [headers],
    [body]
FROM [{0}] 
WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE 
    [recipient] = @recipient 
    AND [expiration] > getdate()
ORDER BY 
    [priority] ASC, 
    [id] asc

", _tableName);

                    selectCommand.Parameters.Add("recipient", SqlDbType.NVarChar, RecipientColumnSize).Value =
                        _inputQueueName;

                    using (var reader = await selectCommand.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return null;

                        var headers = reader["headers"];
                        var headersDictionary = _headerSerializer.Deserialize((byte[])headers);

                        idOfMessageToDelete = (long)reader["id"];
                        var body = (byte[])reader["body"];

                        receivedTransportMessage = new TransportMessage(headersDictionary, body);
                    }
                }

                if (!idOfMessageToDelete.HasValue)
                {
                    return null;
                }

                using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.CommandText = string.Format("DELETE FROM [{0}] WHERE [id] = @id", _tableName);
                    deleteCommand.Parameters.Add("id", SqlDbType.BigInt).Value = idOfMessageToDelete;

                    await deleteCommand.ExecuteNonQueryAsync();
                }

                return receivedTransportMessage;
            }
        }

        int GetMessagePriority(TransportMessage message)
        {
            var valueOrNull = message.Headers.GetValueOrNull(MessagePriorityHeaderKey);
            if (valueOrNull == null) return 0;

            try
            {
                return int.Parse(valueOrNull);
            }
            catch (Exception exception)
            {
                throw new FormatException(string.Format("Could not parse '{0}' into an Int32!", valueOrNull), exception);
            }
        }

        Task<IDbConnection> GetConnection(ITransactionContext context)
        {
            return context
                .GetOrAdd(CurrentConnectionKey,
                    async () =>
                    {
                        var dbConnection = await _connectionProvider.GetConnection();
                        context.OnCommitted(async () => await dbConnection.Complete());
                        context.OnDisposed(() =>
                        {
                            dbConnection.Dispose();
                        });
                        return dbConnection;
                    });
        }

        public string Address
        {
            get { return _inputQueueName; }
        }

        public void CreateQueue(string address)
        {
        }

        /// <summary>
        /// Checks if the table with the configured name exists - if not, it will be created
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionProvider.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(_tableName, StringComparer.OrdinalIgnoreCase))
                {
                    _log.Info("Database already contains a table named '{0}' - will not create anything", _tableName);
                    return;
                }

                _log.Info("Table '{0}' does not exist - it will be created now", _tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}]
(
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[recipient] [nvarchar](200) NOT NULL,
	[priority] [int] NOT NULL,
    [expiration] [datetime2] NOT NULL,
	[headers] [varbinary](max) NOT NULL,
	[body] [varbinary](max) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [recipient] ASC,
	    [priority] ASC,
	    [id] ASC
    )
)
", _tableName);

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

CREATE NONCLUSTERED INDEX [IDX_RECEIVE_{0}] ON [dbo].[{0}]
(
	[recipient] ASC,
	[priority] ASC,
    [expiration] ASC,
	[id] ASC
)

", _tableName);

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

CREATE NONCLUSTERED INDEX [IDX_EXPIRATION_{0}] ON [dbo].[{0}]
(
    [expiration] ASC
)

", _tableName);

                    command.ExecuteNonQuery();
                }

                connection.Complete().Wait();
            }
        }

        /// <summary>
        /// Shuts down the background timer
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Shuts down the background timer
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            try
            {
                _expiredMessagesCleanupTask.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}