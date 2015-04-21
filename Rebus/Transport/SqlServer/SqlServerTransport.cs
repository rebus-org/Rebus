using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.SqlServer;

namespace Rebus.Transport.SqlServer
{
    public class SqlServerTransport : ITransport, IInitializable, IDisposable
    {
        /// <summary>
        /// Special message priority header that can be used with the <see cref="SqlServerTransport"/>. The value must be an <see cref="Int32"/>
        /// </summary>
        public const string MessagePriorityHeaderKey = "rbs2-msg-priority";

        static ILog _log;

        static SqlServerTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        const string CurrentConnectionKey = "sql-server-transport-current-connection";
        const int RecipientColumnSize = 200;

        readonly HeaderSerializer _headerSerializer = new HeaderSerializer();
        readonly DbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly string _inputQueueName;
        
        CancellationTokenSource _expiredMessagesCleanupBackgroundTask;
        ManualResetEvent _expiredMessagesCleanupBackgroundTaskFinished;

        public SqlServerTransport(DbConnectionProvider connectionProvider, string tableName, string inputQueueName)
        {
            _connectionProvider = connectionProvider;
            _tableName = tableName;
            _inputQueueName = inputQueueName;

            ExpiredMessagesCleanupInterval = TimeSpan.FromSeconds(20);
        }

        public void Initialize()
        {
            _expiredMessagesCleanupBackgroundTaskFinished = new ManualResetEvent(false);
            _expiredMessagesCleanupBackgroundTask = StartBackgroundTasks(_expiredMessagesCleanupBackgroundTaskFinished);
        }

        public TimeSpan ExpiredMessagesCleanupInterval { get; set; }

        CancellationTokenSource StartBackgroundTasks(ManualResetEvent finished)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _log.Info("Starting periodic expired messages cleanup for recipient '{0}' with {1} interval", 
                _inputQueueName, ExpiredMessagesCleanupInterval);

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(ExpiredMessagesCleanupInterval, cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                await PerformExpiredMessagesCleanupCycle();
                            }
                            catch (Exception exception)
                            {
                                _log.Warn("Expired messages cleanup experienced an error: {0}", exception);
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    finished.Set();
                    throw;
                }
            }, cancellationToken);

            return cancellationTokenSource;
        }

        async Task PerformExpiredMessagesCleanupCycle()
        {
            var results = 0;
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
            var connection = await GetConnection(context);

            long? idOfMessageToDelete;
            TransportMessage receivedTransportMessage;

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText =
                    string.Format(@"
SELECT TOP 1 [id], [headers], [body]
FROM [{0}]
WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE [recipient] = @recipient
    AND [expiration] > getdate()
ORDER BY [priority] ASC, [id] asc

", _tableName);

                selectCommand.Parameters.Add("recipient", SqlDbType.NVarChar, RecipientColumnSize).Value = _inputQueueName;

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync()) return null;

                    var headers = reader["headers"];
                    var headersDictionary = _headerSerializer.Deserialize((byte[])headers);

                    idOfMessageToDelete = (long)reader["id"];
                    receivedTransportMessage = new TransportMessage(headersDictionary, reader.GetStream(reader.GetOrdinal("body")));
                }
            }

            if (!idOfMessageToDelete.HasValue) return null;

            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.CommandText = string.Format("DELETE FROM [{0}] WHERE [id] = @id", _tableName);
                deleteCommand.Parameters.Add("id", SqlDbType.BigInt).Value = idOfMessageToDelete;
                await deleteCommand.ExecuteNonQueryAsync();
            }

            return receivedTransportMessage;
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

        Task<DbConnection> GetConnection(ITransactionContext context)
        {
            return context.Items
                .GetOrAddAsync(CurrentConnectionKey,
                    async () =>
                    {
                        var dbConnection = await _connectionProvider.GetConnection();
                        context.OnCommitted(async () => await dbConnection.Complete());
                        context.OnAborted(() => dbConnection.Dispose());
                        context.OnDisposed(() => dbConnection.Dispose());
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

        public void Dispose()
        {
            if (_expiredMessagesCleanupBackgroundTask != null)
            {
                _log.Info("Stopping periodic expired messages cleanup for recipient '{0}'", _inputQueueName);
                _expiredMessagesCleanupBackgroundTask.Cancel();
                
                if (!_expiredMessagesCleanupBackgroundTaskFinished.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    _log.Warn("Expired messages background task did not stop within 5 second timeout!!");
                }
            }
        }
    }
}