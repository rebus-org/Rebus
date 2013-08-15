using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using System.Linq;
using Rebus.Serialization;
using IsolationLevel = System.Data.IsolationLevel;

namespace Rebus.Transports.Sql
{
    /// <summary>
    /// SQL Server-based message queue that uses one single table to store all messages. Messages are received in the
    /// way described here: <see cref="http://www.mssqltips.com/sqlservertip/1257/processing-data-queues-in-sql-server-with-readpast-and-updlock/"/>
    /// (which means that the table is queried with a <code>top 1 ... with (updlock, readpast)</code>, allowing for many concurent reads without
    /// unintentional locking).
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
                    // avoid enlisting in ambient tx because we handle this stuff on our own!
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        var connection = new SqlConnection(connectionString);
                        connection.Open();
                        log.Debug("Starting new transaction");
                        connection.BeginTransaction(IsolationLevel.ReadCommitted);
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
                                                            ([recipient], [headers], [label], [body]) 
                                                            values (@recipient, @headers, @label, @body)",
                                                        messageTableName);

                    var id = Guid.NewGuid();

                    log.Debug("Sending message with ID {0} to {1}", id, destinationQueueName);

                    command.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = destinationQueueName;
                    command.Parameters.Add("headers", SqlDbType.NVarChar).Value = DictionarySerializer.Serialize(message.Headers);
                    command.Parameters.Add("label", SqlDbType.NVarChar).Value = message.Label ?? id.ToString();
                    command.Parameters.Add("body", SqlDbType.VarBinary).Value = message.Body;

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
            AssertNotInOneWayClientMode();

            var connection = GetConnectionPossiblyFromContext(context);

            try
            {
                ReceivedTransportMessage receivedTransportMessage = null;

                using (var selectCommand = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(selectCommand);

                    selectCommand.CommandText =
                        string.Format(
                            @"select top 1 [seq], [headers], [label], [body] from [{0}] with (updlock, readpast) where recipient = @recipient order by [seq] asc",
                            messageTableName);

                    selectCommand.Parameters.AddWithValue("recipient", inputQueueName);

                    var seq = 0L;

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var headers = reader["headers"];
                            var label = reader["label"];
                            var body = reader["body"];
                            seq = (long)reader["seq"];

                            var headersDictionary = DictionarySerializer.Deserialize((string)headers);
                            var messageId = string.Format("{0}/{1}", inputQueueName, seq);

                            receivedTransportMessage =
                                new ReceivedTransportMessage
                                    {
                                        Id = messageId,
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

                            deleteCommand.CommandText =
                                string.Format("delete from [{0}] where [recipient] = @recipient and [seq] = @seq",
                                              messageTableName);
                            deleteCommand.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = inputQueueName;
                            deleteCommand.Parameters.Add("seq", SqlDbType.BigInt).Value = seq;

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
	[recipient] [nvarchar](200) NOT NULL,
	[seq] [bigint] IDENTITY(1,1) NOT NULL,
	[label] [nvarchar](max) NOT NULL,
	[headers] [nvarchar](max) NOT NULL,
	[body] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[recipient] ASC,
	[seq] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
", messageTableName);

                    command.ExecuteNonQuery();
                }

//                var indexName = string.Format("IX_{0}", messageTableName);

//                log.Info("Creating index '{0}' on '{1}'", indexName, messageTableName);

//                using (var command = connection.CreateCommand())
//                {
//                    connection.AssignTransactionIfNecessary(command);

//                    command.CommandText = string.Format(@"
//CREATE NONCLUSTERED INDEX [{0}] ON [dbo].[{1}]
//(
//	[recipient] ASC,
//    [seq] ASC
//)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = OFF) ON [PRIMARY]
//", indexName, messageTableName);

//                    command.ExecuteNonQuery();
//                }

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
            AssertNotInOneWayClientMode();

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

        public static SqlServerMessageQueue Sender(string connectionString, string messageTableName)
        {
            return new SqlServerMessageQueue(connectionString, messageTableName, null);
        }

        void AssertNotInOneWayClientMode()
        {
            if (string.IsNullOrWhiteSpace(inputQueueName))
            {
                throw new InvalidOperationException(
                    "This SQL Server message queue is running in one-way client mode - it doesn't have an input queue, so it cannot receive messages and it cannot purge its input queue");
            }
        }
    }
}