using System.Data;
using System;
using System.Data.SqlClient;
using System.Globalization;
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

        readonly Func<ConnectionHolder> getConnection;
        readonly Action<SqlConnection> releaseConnection;
        readonly Action<SqlTransaction> commitAction;
        readonly Action<SqlTransaction> rollbackAction;

        class ConnectionHolder
        {
            public ConnectionHolder(SqlConnection connection, SqlTransaction transaction)
            {
                Connection = connection;
                Transaction = transaction;
            }

            public SqlConnection Connection { get; private set; }
            
            public SqlTransaction Transaction { get; private set; }
            
            public SqlCommand CreateCommand()
            {
                var sqlCommand = Connection.CreateCommand();
                if (Transaction != null)
                {
                    sqlCommand.Transaction = Transaction;
                }
                return sqlCommand;
            }
        }

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
                        var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                        return new ConnectionHolder(connection, transaction);
                    }
                };
            commitAction = t =>
                {
                    if (t == null) return;
                    log.Debug("Committing!");
                    t.Commit();
                };
            rollbackAction = t =>
                {
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
        public SqlServerMessageQueue(Func<Tuple<SqlConnection, SqlTransaction>> connectionFactoryMethod, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = () =>
                {
                    var connectionAndTransaction = connectionFactoryMethod();
                    
                    return new ConnectionHolder(connectionAndTransaction.Item1, connectionAndTransaction.Item2);
                };

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
                    command.CommandText = string.Format(@"insert into [{0}] 
                                                            ([recipient], [headers], [label], [body]) 
                                                            values (@recipient, @headers, @label, @body)",
                                                        messageTableName);

                    var id = Guid.NewGuid();

                    log.Debug("Sending message with label {0} to {1}", destinationQueueName);
                    var label = message.Label ?? "(no label)";

                    command.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = destinationQueueName;
                    command.Parameters.Add("headers", SqlDbType.NVarChar).Value = DictionarySerializer.Serialize(message.Headers);
                    command.Parameters.Add("label", SqlDbType.NVarChar).Value = label;
                    command.Parameters.Add("body", SqlDbType.VarBinary).Value = message.Body;

                    command.ExecuteNonQuery();
                }

                if (!context.IsTransactional)
                {
                    commitAction(connection.Transaction);
                }
            }
            finally
            {
                if (!context.IsTransactional)
                {
                    releaseConnection(connection.Connection);
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

                using (var selectCommand = connection.Connection.CreateCommand())
                {
                    selectCommand.Transaction = connection.Transaction;

                    selectCommand.CommandText =
                        string.Format(
                            @"select top 1 [seq], [headers], [label], [body] from [{0}] 
                                with (updlock, readpast) 
                                where recipient = @recipient order by [seq] asc",
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
                            var messageId = seq.ToString(CultureInfo.InvariantCulture);

                            receivedTransportMessage =
                                new ReceivedTransportMessage
                                    {
                                        Id = messageId,
                                        Label = (string)label,
                                        Headers = headersDictionary,
                                        Body = (byte[])body,
                                    };

                            log.Debug("Received message with ID {0} from logical queue {1}.{2}",
                                      messageId, messageTableName, inputQueueName);
                        }
                    }

                    if (receivedTransportMessage != null)
                    {
                        using (var deleteCommand = connection.Connection.CreateCommand())
                        {
                            deleteCommand.Transaction = connection.Transaction;

                            deleteCommand.CommandText =
                                string.Format("delete from [{0}] where [recipient] = @recipient and [seq] = @seq",
                                              messageTableName);

                            deleteCommand.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = inputQueueName;
                            deleteCommand.Parameters.Add("seq", SqlDbType.BigInt).Value = seq;

                            var rowsAffected = deleteCommand.ExecuteNonQuery();

                            if (rowsAffected != 1)
                            {
                                throw new ApplicationException(
                                    string.Format(
                                        "Attempted to delete message with recipient = '{0}' and seq = {1}, but {2} rows were affected!",
                                        inputQueueName, seq, rowsAffected));
                            }
                        }
                    }
                }

                if (!context.IsTransactional)
                {
                    commitAction(connection.Transaction);
                }

                return receivedTransportMessage;
            }
            finally
            {
                if (!context.IsTransactional)
                {
                    releaseConnection(connection.Connection);
                }
            }
        }

        ConnectionHolder GetConnectionPossiblyFromContext(ITransactionContext context)
        {
            if (!context.IsTransactional)
            {
                return getConnection();
            }

            if (context[ConnectionKey] != null) return (ConnectionHolder)context[ConnectionKey];

            var connectionAndTransaction = getConnection();

            var sqlConnection = connectionAndTransaction.Connection;

            context.DoCommit += () => commitAction(connectionAndTransaction.Transaction);
            context.DoRollback += () => rollbackAction(connectionAndTransaction.Transaction);
            context.Cleanup += () => releaseConnection(sqlConnection);

            context[ConnectionKey] = connectionAndTransaction;

            return connectionAndTransaction;
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
                var tableNames = connection.Connection.GetTableNames();

                if (tableNames.Contains(messageTableName, StringComparer.OrdinalIgnoreCase))
                {
                    log.Info("Database already contains a table named '{0}' - will not create anything",
                             messageTableName);
                    commitAction(connection.Transaction);
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", messageTableName);

                using (var command = connection.Connection.CreateCommand())
                {
                    command.Transaction = connection.Transaction;

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

                commitAction(connection.Transaction);
            }
            finally
            {
                releaseConnection(connection.Connection);
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
                using (var command = connection.Connection.CreateCommand())
                {
                    command.Transaction = connection.Transaction;

                    command.CommandText = string.Format(@"delete from [{0}] where recipient = @recipient",
                                                        messageTableName);

                    command.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = inputQueueName;

                    command.ExecuteNonQuery();
                }

                commitAction(connection.Transaction);
            }
            finally
            {
                releaseConnection(connection.Connection);
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