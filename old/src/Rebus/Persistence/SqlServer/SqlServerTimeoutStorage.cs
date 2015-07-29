using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Logging;
using Rebus.Timeout;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementaion of <see cref="IStoreTimeouts"/> that uses an SQL Server to store the timeouts
    /// </summary>
    public class SqlServerTimeoutStorage : IStoreTimeouts
    {
        static ILog log;

        static SqlServerTimeoutStorage()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string connectionString;
        readonly string timeoutsTableName;

        /// <summary>
        /// Constructs the timeout storage which will use the specified connection string to connect to a database,
        /// storing the timeouts in the table with the specified name
        /// </summary>
        public SqlServerTimeoutStorage(string connectionString, string timeoutsTableName)
        {
            this.connectionString = connectionString;
            this.timeoutsTableName = timeoutsTableName;
        }

        /// <summary>
        /// Gets the name of the table where timeouts are stored
        /// </summary>
        public string TimeoutsTableName
        {
            get { return timeoutsTableName; }
        }

        /// <summary>
        /// Adds the given timeout to the table specified by <see cref="TimeoutsTableName"/>
        /// </summary>
        public void Add(Timeout.Timeout newTimeout)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    var parameters =
                        new List<Tuple<string, object, SqlDbType>>
                            {
                                Tuple.Create("time_to_return", (object)newTimeout.TimeToReturn, SqlDbType.DateTime2),
                                Tuple.Create("correlation_id", (object)newTimeout.CorrelationId, SqlDbType.NVarChar),
                                Tuple.Create("saga_id", (object)newTimeout.SagaId, SqlDbType.UniqueIdentifier),
                                Tuple.Create("reply_to", (object)newTimeout.ReplyTo, SqlDbType.NVarChar),
                            };

                    if (newTimeout.CustomData != null)
                    {
                        parameters.Add(Tuple.Create("custom_data", (object)newTimeout.CustomData, SqlDbType.NVarChar));
                    }
                    // generate sql with necessary columns including matching sql parameter names
                    command.CommandText =
                        string.Format(
                            @"insert into [{0}] ({1}) values ({2})",
                            timeoutsTableName,
                            string.Join(", ", parameters.Select(c => c.Item1)),
                            string.Join(", ", parameters.Select(c => "@" + c.Item1)));

                    // set parameters
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter.Item1, parameter.Item3).Value = parameter.Item2;
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Queries the underlying table and returns due timeouts, removing them at the same time
        /// </summary>
        public DueTimeoutsResult GetDueTimeouts()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var transaction = connection.BeginTransaction();

            var dueTimeouts = new List<DueTimeout>();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    string.Format(
                        @"
select 
    id, 
    time_to_return, 
    correlation_id, 
    saga_id, 
    reply_to, 
    custom_data 

from [{0}] with (updlock, readpast, rowlock)

where time_to_return <= @current_time 

order by time_to_return asc
",
                        timeoutsTableName);

                command.Parameters.AddWithValue("current_time", RebusTimeMachine.Now());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = (long)reader["id"];
                        var correlationId = (string)reader["correlation_id"];
                        var sagaId = (Guid)reader["saga_id"];
                        var replyTo = (string)reader["reply_to"];
                        var timeToReturn = (DateTime)reader["time_to_return"];
                        var customData = (string)(reader["custom_data"] != DBNull.Value ? reader["custom_data"] : "");

                        var sqlTimeout = new DueSqlTimeout(id, replyTo, correlationId, timeToReturn, sagaId, customData, timeoutsTableName, connection, transaction);

                        dueTimeouts.Add(sqlTimeout);
                    }
                }

                return new DueTimeoutsResult(dueTimeouts, () =>
                {
                    transaction.Commit();
                    transaction.Dispose();
                    connection.Dispose();
                });
            }
        }

        /// <summary>
        /// Creates the necessary timeout storage table if it hasn't already been created. If a table already exists
        /// with a name that matches the desired table name, no action is performed (i.e. it is assumed that
        /// the table already exists).
        /// </summary>
        public SqlServerTimeoutStorage EnsureTableIsCreated()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(timeoutsTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", timeoutsTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

CREATE TABLE [dbo].[{0}](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
	[time_to_return] [datetime2](7) NOT NULL,
	[correlation_id] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
	[reply_to] [nvarchar](200) NOT NULL,
	[custom_data] [nvarchar](MAX) NULL
    CONSTRAINT [PK_{0}] PRIMARY KEY NONCLUSTERED 
    (
	    [id] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

", timeoutsTableName);
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

CREATE CLUSTERED INDEX [IX_{0}_TimeToReturn] ON [dbo].[{0}]
(
	[time_to_return] ASC
)

", timeoutsTableName);
                    command.ExecuteNonQuery();
                }
            }

            return this;
        }

        class DueSqlTimeout : DueTimeout
        {
            readonly string timeoutsTableName;
            readonly SqlConnection connection;
            readonly SqlTransaction transaction;
            readonly long id;

            public DueSqlTimeout(long id, string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, string timeoutsTableName, SqlConnection connection, SqlTransaction transaction)
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.id = id;
                this.timeoutsTableName = timeoutsTableName;
                this.connection = connection;
                this.transaction = transaction;
            }

            public override void MarkAsProcessed()
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = string.Format(@"delete from [{0}] where id = @id", timeoutsTableName);
                    command.Parameters.Add("id", SqlDbType.BigInt).Value = id;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}