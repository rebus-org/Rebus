using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Logging;
using Rebus.Timeout;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementaion of <see cref="IStoreTimeouts"/> that uses an SQL Server to store the timeouts
    /// </summary>
    public class SqlServerTimeoutStorage : SqlServerMagic, IStoreTimeouts
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
                        new List<Tuple<string, object>>
                            {
                                new Tuple<string, object>("time_to_return", newTimeout.TimeToReturn),
                                new Tuple<string, object>("correlation_id", newTimeout.CorrelationId),
                                new Tuple<string, object>("saga_id", newTimeout.SagaId),
                                new Tuple<string, object>("reply_to", newTimeout.ReplyTo)
                            };

                    if (newTimeout.CustomData != null)
                    {
                        parameters.Add(new Tuple<string, object>("custom_data", newTimeout.CustomData));
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
                        command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        // if we're violating PK, it's because we're inserting the same timeout again...
                        if (ex.Number != PrimaryKeyViolationNumber) throw;
                    }
                }
            }
        }

        /// <summary>
        /// Queries the underlying table and returns due timeouts, removing them at the same time
        /// </summary>
        public IEnumerable<DueTimeout> GetDueTimeouts()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var dueTimeouts = new List<DueTimeout>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            @"select time_to_return, correlation_id, saga_id, reply_to, custom_data from [{0}] where time_to_return <= @current_time",
                            timeoutsTableName);

                    command.Parameters.AddWithValue("current_time", RebusTimeMachine.Now());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var correlationId = (string)reader["correlation_id"];
                            var sagaId = (Guid)reader["saga_id"];
                            var replyTo = (string)reader["reply_to"];
                            var timeToReturn = (DateTime)reader["time_to_return"];
                            var customData = (string)(reader["custom_data"] != DBNull.Value ? reader["custom_data"] : "");

                            var sqlTimeout = new DueSqlTimeout(replyTo, correlationId, timeToReturn, sagaId, customData, connectionString, timeoutsTableName);

                            dueTimeouts.Add(sqlTimeout);
                        }
                    }

                }

                return dueTimeouts;
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
	[time_to_return] [datetime] NOT NULL,
	[correlation_id] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
	[reply_to] [nvarchar](200) NOT NULL,
	[custom_data] [nvarchar](MAX) NULL,
 CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[time_to_return] ASC,
	[correlation_id] ASC,
	[reply_to] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
", timeoutsTableName);
                    command.ExecuteNonQuery();
                }
            }

            return this;
        }

        class DueSqlTimeout : DueTimeout
        {
            readonly string connectionString;
            readonly string timeoutsTableName;

            public DueSqlTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, string connectionString, string timeoutsTableName)
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.connectionString = connectionString;
                this.timeoutsTableName = timeoutsTableName;
            }

            public override void MarkAsProcessed()
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            string.Format(
                                @"delete from [{0}] 
                          where time_to_return = @time_to_return 
                            and reply_to = @reply_to
                            and correlation_id = @correlation_id",
                                timeoutsTableName);

                        command.Parameters.AddWithValue("time_to_return", TimeToReturn);
                        command.Parameters.AddWithValue("correlation_id", CorrelationId);
                        command.Parameters.AddWithValue("reply_to", ReplyTo);

                        var executeNonQuery = command.ExecuteNonQuery();

                        if (executeNonQuery == 0)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "Stale state! Attempted to delete {0} from {1}, but it was already deleted!",
                                    this, timeoutsTableName));
                        }
                    }
                }
            }
        }
    }
}