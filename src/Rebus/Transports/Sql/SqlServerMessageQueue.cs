using System;
using System.Data.SqlClient;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using System.Linq;
using Rebus.Serialization;

namespace Rebus.Transports.Sql
{
    public class SqlServerMessageQueue : SqlServerMagic, IDuplexTransport, IDisposable
    {
        static ILog log;

        static SqlServerMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static readonly DictionarySerializer DictionarySerializer = new DictionarySerializer();

        readonly string messageTableName;
        readonly string inputQueueName;

        readonly Func<SqlConnection> getConnection;
        readonly Action<SqlConnection> releaseConnection;

        public SqlServerMessageQueue(string connectionString, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = () =>
                {
                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    return connection;
                };
            releaseConnection = c => c.Dispose();
        }

        public SqlServerMessageQueue(Func<SqlConnection> connectionFactoryMethod, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = connectionFactoryMethod;
            releaseConnection = c => { };
        }

        SqlServerMessageQueue(string messageTableName, string inputQueueName)
        {
            this.messageTableName = messageTableName;
            this.inputQueueName = inputQueueName;
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    AssignTransactionIfNecessary(connection, command);

                    command.CommandText = string.Format(@"insert into [{0}] 
                                                ([id], [headers], [label], [body], [recipient]) 
                                                values (@id, @headers, @label, @body, @recipient)",
                                                        messageTableName);

                    var id = Guid.NewGuid();
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("headers", DictionarySerializer.Serialize(message.Headers));
                    command.Parameters.AddWithValue("label", message.Label ?? id.ToString());
                    command.Parameters.AddWithValue("body", message.Body);
                    command.Parameters.AddWithValue("recipient", destinationQueueName);

                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                releaseConnection(connection);
            }

        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    AssignTransactionIfNecessary(connection, command);

                    command.CommandText = string.Format(@"select top 1 [id], headers, label, body from [{0}] where recipient = @recipient",
                                                        messageTableName);

                    command.Parameters.AddWithValue("recipient", inputQueueName);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return
                                new ReceivedTransportMessage
                                    {
                                        Id = (reader["id"]).ToString(),
                                        Label = (string) reader["label"],
                                        Headers = DictionarySerializer.Deserialize((string) reader["headers"]),
                                        Body = (byte[]) reader["body"],
                                    };
                        }
                    }
                }
            }
            finally
            {
                releaseConnection(connection);
            }

            return null;
        }

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

        public SqlServerMessageQueue EnsureTableIsCreated()
        {
            var connection = getConnection();
            try
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(messageTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", messageTableName);

                using (var command = connection.CreateCommand())
                {
                    AssignTransactionIfNecessary(connection, command);

                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}](
	[id] [uniqueidentifier] NOT NULL,
	[label] [nvarchar](max) NOT NULL,
	[headers] [nvarchar](max) NOT NULL,
	[body] [varbinary](max) NOT NULL,
	[recipient] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
", messageTableName);

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    AssignTransactionIfNecessary(connection, command);

                    command.CommandText = string.Format(@"
CREATE NONCLUSTERED INDEX [IX_{0}] ON [dbo].[{0}]
(
	[recipient] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
", messageTableName);

                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                releaseConnection(connection);
            }
            return this;
        }

        public void Dispose()
        {
        }

        public SqlServerMessageQueue PurgeInputQueue()
        {
            log.Warn("Purging queue {0} in table {1}", inputQueueName, messageTableName);

            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    AssignTransactionIfNecessary(connection, command);

                    command.CommandText = string.Format(@"delete from [{0}]
                                                where recipient = @recipient",
                                                        messageTableName);

                    command.Parameters.AddWithValue("recipient", inputQueueName);

                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                releaseConnection(connection);
            }

            return this;
        }
    }
}