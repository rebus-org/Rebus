using System;
using System.Data.SqlClient;
using System.Transactions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using System.Linq;
using Rebus.Serialization;

namespace Rebus.Transports.Sql
{
    /// <summary>
    /// http://www.mssqltips.com/sqlservertip/1257/processing-data-queues-in-sql-server-with-readpast-and-updlock/
    /// </summary>
    public class SqlServerMessageQueue : IDuplexTransport, IDisposable
    {
        const string ConnectionKey = "sql-server-message-queue-current-connection";
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
        readonly Action<SqlConnection> commitAction;
        readonly Action<SqlConnection> rollbackAction;

        /// <summary>
        /// Constructs the SQL Server-based Rebus transport using the specified <see cref="connectionString"/> to connect to a database,
        /// storing messages in the table with the specified name, using <see cref="inputQueueName"/> as the logical input queue name
        /// when receiving messages.
        /// </summary>
        public SqlServerMessageQueue(string connectionString, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = () =>
                {
                    using (var suppressAmbientTransaction = new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        var connection = new SqlConnection(connectionString);
                        connection.Open();
                        log.Debug("Starting new transaction");
                        connection.BeginTransaction();
                        return connection;
                    }
                };
            commitAction = c =>
                {
                    var t = c.GetTransactionOrNull();
                    if (t == null) return;

                    log.Debug("Committing!");
                    t.Commit();
                };
            rollbackAction = c =>
                {
                    var t = c.GetTransactionOrNull();
                    if (t == null) return;

                    log.Debug("Rolling back!");
                    t.Rollback();
                };
            releaseConnection = c =>
                {
                    log.Debug("Disposing connection");
                    c.Dispose();
                };
        }

        /// <summary>
        /// Constructs the SQL Server-based Rebus transport using the specified factory method to obtain a connection to a database,
        /// storing messages in the table with the specified name, using <see cref="inputQueueName"/> as the logical input queue name
        /// when receiving messages.
        /// </summary>
        public SqlServerMessageQueue(Func<SqlConnection> connectionFactoryMethod, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = connectionFactoryMethod;

            // everything else is handed over to whoever provided the connection
            releaseConnection = c => { };
            commitAction = c => { };
            rollbackAction = c => { };
        }

        SqlServerMessageQueue(string messageTableName, string inputQueueName)
        {
            this.messageTableName = messageTableName;
            this.inputQueueName = inputQueueName;
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var connection = GetConnectionPossiblyFromContext(context);

            try
            {
                using (var command = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(command);

                    command.CommandText = string.Format(@"insert into [{0}] 
                                                ([id], [headers], [label], [body], [recipient]) 
                                                values (@id, @headers, @label, @body, @recipient)",
                                                        messageTableName);

                    var id = Guid.NewGuid();

                    log.Debug("Sending message with ID {0} to {1}", id, destinationQueueName);

                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("headers", DictionarySerializer.Serialize(message.Headers));
                    command.Parameters.AddWithValue("label", message.Label ?? id.ToString());
                    command.Parameters.AddWithValue("body", message.Body);
                    command.Parameters.AddWithValue("recipient", destinationQueueName);

                    command.ExecuteNonQuery();
                }

                if (!context.IsTransactional)
                {
                    commitAction(connection);
                }
            }
            finally
            {
                if (!context.IsTransactional)
                {
                    releaseConnection(connection);
                }
            }
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var connection = GetConnectionPossiblyFromContext(context);

            try
            {
                ReceivedTransportMessage receivedTransportMessage = null;

                using (var selectCommand = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(selectCommand);

                    selectCommand.CommandText =
                        string.Format(
                            @"select top 1 [id], [headers], [label], [body] from [{0}] with (updlock, readpast) where recipient = @recipient ",
                            messageTableName);

                    selectCommand.Parameters.AddWithValue("recipient", inputQueueName);

                    var messageId = Guid.Empty;

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            messageId = (Guid)reader["id"];
                            var headers = reader["headers"];
                            var label = reader["label"];
                            var body = reader["body"];

                            var headersDictionary = DictionarySerializer.Deserialize((string)headers);

                            receivedTransportMessage =
                                new ReceivedTransportMessage
                                    {
                                        Id = messageId.ToString(),
                                        Label = (string)label,
                                        Headers = headersDictionary,
                                        Body = (byte[])body,
                                    };

                            log.Debug("Received message with ID {0} from {1}", messageId, inputQueueName);
                        }
                    }

                    if (receivedTransportMessage != null)
                    {
                        using (var deleteCommand = connection.CreateCommand())
                        {
                            connection.AssignTransactionIfNecessary(deleteCommand);

                            deleteCommand.CommandText = string.Format("delete from [{0}] where [id] = @id",
                                                                      messageTableName);
                            deleteCommand.Parameters.AddWithValue("id", messageId);
                            deleteCommand.ExecuteNonQuery();
                        }
                    }
                }

                if (!context.IsTransactional)
                {
                    commitAction(connection);
                }

                return receivedTransportMessage;
            }
            finally
            {
                if (!context.IsTransactional)
                {
                    releaseConnection(connection);
                }
            }
        }

        SqlConnection GetConnectionPossiblyFromContext(ITransactionContext context)
        {
            if (!context.IsTransactional) return getConnection();

            if (context[ConnectionKey] != null) return (SqlConnection)context[ConnectionKey];

            var sqlConnection = getConnection();

            context.DoCommit += () => commitAction(sqlConnection);
            context.DoRollback += () => rollbackAction(sqlConnection);
            context.Cleanup += () => releaseConnection(sqlConnection);

            context[ConnectionKey] = sqlConnection;

            return sqlConnection;
        }

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

        /// <summary>
        /// Creates the message table if a table with that name does not already exist
        /// </summary>
        public SqlServerMessageQueue EnsureTableIsCreated()
        {
            var connection = getConnection();
            try
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(messageTableName, StringComparer.OrdinalIgnoreCase))
                {
                    log.Info("Database already contains a table named '{0}' - will not create anything",
                             messageTableName);
                    commitAction(connection);
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", messageTableName);

                using (var command = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(command);

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

                var indexName = string.Format("IX_{0}", messageTableName);

                log.Info("Creating index '{0}' on '{1}'", indexName, messageTableName);

                using (var command = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(command);

                    command.CommandText = string.Format(@"
CREATE NONCLUSTERED INDEX [{0}] ON [dbo].[{1}]
(
	[recipient] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
", indexName, messageTableName);

                    command.ExecuteNonQuery();
                }

                commitAction(connection);
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

        /// <summary>
        /// Deletes all the messages from the message table that have the current input queue specified as the recipient
        /// </summary>
        public SqlServerMessageQueue PurgeInputQueue()
        {
            log.Warn("Purging queue '{0}' in table '{1}'", inputQueueName, messageTableName);

            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(command);

                    command.CommandText = string.Format(@"delete from [{0}] where recipient = @recipient",
                                                        messageTableName);

                    command.Parameters.AddWithValue("recipient", inputQueueName);

                    command.ExecuteNonQuery();
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }

            return this;
        }
    }
}