using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.SqlServer;

namespace Rebus.Transport.SqlServer
{
    public class SqlServerTransport : ITransport
    {
        static ILog _log;

        static SqlServerTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        const string CurrentConnectionKey = "sql-server-transport-current-connection";
        readonly HeaderSerializer _headerSerializer = new HeaderSerializer();
        readonly DbConnectionProvider _connectionProvider;
        readonly string _tableName;
        readonly string _inputQueueName;

        public SqlServerTransport(DbConnectionProvider connectionProvider, string tableName, string inputQueueName)
        {
            _connectionProvider = connectionProvider;
            _tableName = tableName;
            _inputQueueName = inputQueueName;
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
    [priority]
) 
VALUES (
    @recipient,
    @headers,
    @body,
    @priority
)",
                    _tableName);

                var priority = GetMessagePriority(message);

                command.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = destinationAddress;
                command.Parameters.Add("headers", SqlDbType.VarBinary).Value = _headerSerializer.Serialize(message.Headers);
                command.Parameters.Add("body", SqlDbType.VarBinary).Value = message.Body;
                command.Parameters.Add("priority", SqlDbType.TinyInt, 1).Value = priority;

                command.ExecuteNonQuery();
            }
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

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText =
                    string.Format(@"
SELECT TOP 1 [seq], [headers], [body], [priority]
FROM [{0}]
WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE [recipient] = @recipient
ORDER BY [priority] ASC, [seq] asc

", _tableName);

                selectCommand.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = _tableName;

                using (var reader = selectCommand.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var headers = reader["headers"];
                    var seq = (long)reader["seq"];

                    var headersDictionary = _headerSerializer.Deserialize((byte[])headers);
                    var messageId = seq.ToString(CultureInfo.InvariantCulture);

                    var receivedTransportMessage = new TransportMessage(headersDictionary, reader.GetStream(reader.GetOrdinal("body")));

                    return receivedTransportMessage;
                }
            }
        }

        int GetMessagePriority(object message)
        {
            return 0;
        }

        Task<DbConnection> GetConnection(ITransactionContext context)
        {
            return context.Items
                .GetOrAddAsync(CurrentConnectionKey,
                    async () =>
                    {
                        var dbConnection = await _connectionProvider.GetConnection();
                        context.Committed += dbConnection.Complete;
                        context.Cleanup += dbConnection.Dispose;
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
CREATE TABLE [dbo].[{0}](
	[recipient] [nvarchar](200) NOT NULL,
	[seq] [bigint] IDENTITY(1,1) NOT NULL,
	[priority] [tinyint] NOT NULL,
	[headers] [varbinary](max) NOT NULL,
	[body] [varbinary](max) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [recipient] ASC,
	    [priority] ASC,
	    [seq] ASC
    )
)
", _tableName);

                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }
    }
}