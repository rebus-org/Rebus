using System.Data;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Transactions;
using Rebus.Logging;
using System.Linq;
using Rebus.Serialization;
using IsolationLevel = System.Data.IsolationLevel;

namespace Rebus.Transports.Sql
{
    /// <summary>
    /// SQL Server-based message queue that uses one single table to store all messages. Messages are received in the
    /// way described here: http://www.mssqltips.com/sqlservertip/1257/processing-data-queues-in-sql-server-with-readpast-and-updlock/
    /// (which means that the table is queried with a <code>top 1 ... with (updlock, readpast)</code>, allowing for many concurent reads without
    /// unintentional locking).
    /// (alternative implementation: http://stackoverflow.com/questions/10820105/t-sql-delete-except-top-1)
    /// </summary>
    public class SqlServerMessageQueue : IDuplexTransport
    {
        /// <summary>
        /// The default priority that messages will have if the priority has not explicitly been set to something else
        /// </summary>
        public const int DefaultMessagePriority = 128;

        /// <summary>
        /// Special header key that can be used to set the priority of a sent transport message. Please note that the
        /// priority must be an integer value in the range [0;255] since it is mapped to a tinyint in the database.
        /// </summary>
        public const string PriorityHeaderKey = "rebus-sql-message-priority";

        const string ConnectionKey = "sql-server-message-queue-current-connection";
        const int Max = -1;
        static ILog log;

        static SqlServerMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static readonly DictionarySerializer DictionarySerializer = new DictionarySerializer();

        readonly string messageTableName;
        readonly string inputQueueName;

        readonly Func<ConnectionHolder> getConnection;
        readonly Action<ConnectionHolder> commitAction;
        readonly Action<ConnectionHolder> rollbackAction;
        readonly Action<ConnectionHolder> releaseConnection;

        /// <summary>
        /// Constructs the SQL Server-based Rebus transport using the specified <paramref name="connectionString"/> to connect to a database,
        /// storing messages in the table with the specified name, using <paramref cref="inputQueueName"/> as the logical input queue name
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
                    var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    return ConnectionHolder.ForTransactionalWork(connection, transaction);
                }
            };
            commitAction = h => h.Commit();
            rollbackAction = h => h.RollBack();
            releaseConnection = h => h.Dispose();
        }

        /// <summary>
        /// Constructs the SQL Server-based Rebus transport using the specified factory method to obtain a connection to a database,
        /// storing messages in the table with the specified name, using <see cref="inputQueueName"/> as the logical input queue name
        /// when receiving messages.
        /// </summary>
        public SqlServerMessageQueue(Func<ConnectionHolder> connectionFactoryMethod, string messageTableName, string inputQueueName)
            : this(messageTableName, inputQueueName)
        {
            getConnection = connectionFactoryMethod;

            // everything else is handed over to whoever provided the connection
            releaseConnection = h => { };
            commitAction = h => { };
            rollbackAction = h => { };
        }

        SqlServerMessageQueue(string messageTableName, string inputQueueName)
        {
            this.messageTableName = messageTableName;
            this.inputQueueName = inputQueueName;
        }

        /// <summary>
        /// Sends the specified <see cref="TransportMessageToSend"/> to the logical queue specified by <paramref name="destinationQueueName"/>.
        /// What actually happens, is that a row is inserted into the messages table, setting the 'recipient' column to the specified
        /// queue.
        /// </summary>
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var connection = GetConnectionPossiblyFromContext(context);

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"insert into [{0}] 
                                                            ([recipient], [headers], [label], [body], [priority]) 
                                                            values (@recipient, @headers, @label, @body, @priority)",
                                                        messageTableName);

                    var label = message.Label ?? "(no label)";
                    log.Debug("Sending message with label {0} to {1}", label, destinationQueueName);

                    var priority = GetMessagePriority(message);

                    command.Parameters.Add("recipient", SqlDbType.NVarChar, 200).Value = destinationQueueName;
                    command.Parameters.Add("headers", SqlDbType.NVarChar, Max).Value = DictionarySerializer.Serialize(message.Headers);
                    command.Parameters.Add("label", SqlDbType.NVarChar, Max).Value = label;
                    command.Parameters.Add("body", SqlDbType.VarBinary, Max).Value = message.Body;
                    command.Parameters.Add("priority", SqlDbType.TinyInt, 1).Value = priority;

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

        static int GetMessagePriority(TransportMessageToSend message)
        {
            if (!message.Headers.ContainsKey(PriorityHeaderKey))
                return DefaultMessagePriority;

            var priorityAsString = message.Headers[PriorityHeaderKey].ToString();

            try
            {
                var priority = int.Parse(priorityAsString);

                if (priority < 0 || priority > 255)
                {
                    throw new ArgumentException(string.Format("Message priority out of range: {0}", priority));
                }

                return priority;
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    string.Format(
                        "Could not decode message priority '{0}' - message priority must be an integer value in the [0;255] range",
                        priorityAsString), exception);
            }
        }

        /// <summary>
        /// Receives a message from the logical queue specified as this instance's input queue. What actually
        /// happens, is that a row is read and locked in the messages table, whereafter it is deleted.
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                AssertNotInOneWayClientMode();

                var connection = GetConnectionPossiblyFromContext(context);

                try
                {
                    ReceivedTransportMessage receivedTransportMessage = null;

                    using (var selectCommand = connection.Connection.CreateCommand())
                    {
                        selectCommand.Transaction = connection.Transaction;

                        //                    selectCommand.CommandText =
                        //                        string.Format(
                        //                            @"
                        //                                ;with msg as (
                        //	                                select top 1 [seq], [headers], [label], [body]
                        //		                                from [{0}]
                        //                                        with (updlock, readpast, rowlock)
                        //		                                where [recipient] = @recipient
                        //		                                order by [seq] asc
                        //	                                )
                        //                                delete msg
                        //                                output deleted.seq, deleted.headers, deleted.body, deleted.label
                        //",
                        //                            messageTableName);

                        selectCommand.CommandText =
                            string.Format(@"
                                    select top 1 [seq], [headers], [label], [body], [priority]
		                                from [{0}]
                                        with (updlock, readpast, rowlock)
		                                where [recipient] = @recipient
		                                order by [priority] asc, [seq] asc
", messageTableName);

                        //                    selectCommand.CommandText =
                        //                        string.Format(@"
                        //delete top(1) from [{0}] 
                        //output deleted.seq, deleted.headers, deleted.body, deleted.label
                        //where [seq] = (
                        //    select min([seq]) from [{0}] with (readpast, holdlock) where recipient = @recipient
                        //)
                        //", messageTableName);

                        selectCommand.Parameters.Add("recipient", SqlDbType.NVarChar, 200)
                                     .Value = inputQueueName;

                        var seq = 0L;
                        var priority = -1;

                        using (var reader = selectCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var headers = reader["headers"];
                                var label = reader["label"];
                                var body = reader["body"];
                                seq = (long) reader["seq"];
                                priority = (byte) reader["priority"];

                                var headersDictionary = DictionarySerializer.Deserialize((string) headers);
                                var messageId = seq.ToString(CultureInfo.InvariantCulture);

                                receivedTransportMessage =
                                    new ReceivedTransportMessage
                                        {
                                            Id = messageId,
                                            Label = (string) label,
                                            Headers = headersDictionary,
                                            Body = (byte[]) body,
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
                                    string.Format(
                                        "delete from [{0}] where [recipient] = @recipient and [priority] = @priority and [seq] = @seq",
                                        messageTableName);

                                deleteCommand.Parameters.Add("recipient", SqlDbType.NVarChar, 200)
                                             .Value = inputQueueName;
                                deleteCommand.Parameters.Add("seq", SqlDbType.BigInt, 8)
                                             .Value = seq;
                                deleteCommand.Parameters.Add("priority", SqlDbType.TinyInt, 1)
                                             .Value = priority;

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
            catch (Exception exception)
            {
                // if we end up here, something bas has happened - no need to hurry, so we sleep
                Thread.Sleep(2000);

                throw new ApplicationException(
                    string.Format("An error occurred while receiving message from {0}", inputQueueName),
                    exception);
            }
        }

        ConnectionHolder GetConnectionPossiblyFromContext(ITransactionContext context)
        {
            if (!context.IsTransactional)
            {
                return getConnection();
            }

            if (context[ConnectionKey] != null) return (ConnectionHolder)context[ConnectionKey];

            var connection = getConnection();

            context.DoCommit += () => commitAction(connection);
            context.DoRollback += () => rollbackAction(connection);
            context.Cleanup += () => releaseConnection(connection);

            context[ConnectionKey] = connection;

            return connection;
        }

        /// <summary>
        /// Gets the name of this receiver's input queue - i.e. this is the queue that this receiver
        /// will pull messages from.
        /// </summary>
        public string InputQueue { get { return inputQueueName; } }

        /// <summary>
        /// Gets the globally accessible adddress of this receiver's input queue - i.e. this would probably
        /// be the input queue in some form, possible qualified by machine name or something similar.
        /// </summary>
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

                using (var command = connection.Connection.CreateCommand())
                {
                    command.Transaction = connection.Transaction;

                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}](
	[recipient] [nvarchar](200) NOT NULL,
	[seq] [bigint] IDENTITY(1,1) NOT NULL,
	[priority] [tinyint] NOT NULL,
	[label] [nvarchar](max) NOT NULL,
	[headers] [nvarchar](max) NOT NULL,
	[body] [varbinary](max) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [recipient] ASC,
	    [priority] ASC,
	    [seq] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = OFF)
)
", messageTableName);

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

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }

            return this;
        }

        /// <summary>
        /// Creates a <see cref="SqlServerMessageQueue"/> that is capable of sending only.
        /// </summary>
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