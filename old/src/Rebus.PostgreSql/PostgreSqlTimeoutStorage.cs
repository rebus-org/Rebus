using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Rebus.Logging;
using Rebus.Timeout;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Implements a timeout storage for Rebus that stores sagas in PostgreSql.
    /// </summary>
    public class PostgreSqlTimeoutStorage : PostgreSqlStorage, IStoreTimeouts
    {
        static ILog log;

        readonly string timeoutsTableName;

        static PostgreSqlTimeoutStorage()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Constructs the timeout storage which will use the specified connection string to connect to a database,
        /// storing the timeouts in the table with the specified name
        /// </summary>
        public PostgreSqlTimeoutStorage(string connectionString, string timeoutsTableName)
            : base(connectionString)
        {
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
            var connection = getConnection();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "time_to_return", newTimeout.TimeToReturn },
                        { "correlation_id", newTimeout.CorrelationId },
                        { "saga_id", newTimeout.SagaId },
                        { "reply_to", newTimeout.ReplyTo }
                    };

                    if (newTimeout.CustomData != null)
                    {
                        parameters.Add("custom_data", newTimeout.CustomData);
                    }

                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    const string sql = @"INSERT INTO ""{0}"" ({1}) VALUES ({2})";

                    var valueNames = string.Join(", ", parameters.Keys.Select(x => "\"" + x + "\""));
                    var parameterNames = string.Join(", ", parameters.Keys.Select(x => "@" + x));

                    command.CommandText = string.Format(sql, timeoutsTableName, valueNames, parameterNames);

                    command.ExecuteNonQuery();
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Queries the underlying table and returns due timeouts, removing them at the same time
        /// </summary>
        public DueTimeoutsResult GetDueTimeouts()
        {
            var dueTimeouts = new List<DueTimeout>();
            var connection = getConnection();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    const string sql = @"
SELECT ""id"", ""time_to_return"", ""correlation_id"", ""saga_id"", ""reply_to"", ""custom_data""
FROM ""{0}""
WHERE ""time_to_return"" <= @current_time
ORDER BY ""time_to_return"" ASC
";

                    command.CommandText = string.Format(sql, timeoutsTableName);

                    command.Parameters.AddWithValue("current_time", RebusTimeMachine.Now());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var sqlTimeout = DuePostgreSqlTimeout.Create(MarkAsProcessed, timeoutsTableName, reader);

                            dueTimeouts.Add(sqlTimeout);
                        }
                    }
                }

                connection.Commit();
            }
            finally
            {
                releaseConnection(connection);
            }

            return new DueTimeoutsResult(dueTimeouts);
        }

        void MarkAsProcessed(DuePostgreSqlTimeout dueTimeout)
        {
            var connection = getConnection();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"DELETE FROM ""{0}"" WHERE ""id"" = @id", timeoutsTableName);

                    command.Parameters.AddWithValue("id", dueTimeout.Id);

                    command.ExecuteNonQuery();
                }

                connection.Commit();
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Creates the necessary timeout storage table if it hasn't already been created. If a table already exists
        /// with a name that matches the desired table name, no action is performed (i.e. it is assumed that
        /// the table already exists).
        /// </summary>
        public PostgreSqlTimeoutStorage EnsureTableIsCreated()
        {
            var connection = getConnection();
            try
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(timeoutsTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", timeoutsTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE ""{0}"" (
    ""id"" BIGSERIAL NOT NULL,
    ""time_to_return"" TIMESTAMP WITH TIME ZONE NOT NULL,
    ""correlation_id"" VARCHAR(200) NOT NULL,
    ""saga_id"" UUID NOT NULL,
    ""reply_to"" VARCHAR(200) NOT NULL,
    ""custom_data"" TEXT NULL,
    PRIMARY KEY (""id"")
);
", timeoutsTableName);

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE INDEX ON ""{0}"" (""time_to_return"");
", timeoutsTableName);

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

        public class DuePostgreSqlTimeout : DueTimeout
        {
            readonly Action<DuePostgreSqlTimeout> markAsProcessedAction;
            readonly string timeoutsTableName;

            readonly long id;

            public DuePostgreSqlTimeout(Action<DuePostgreSqlTimeout> markAsProcessedAction, string timeoutsTableName, long id, string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData)
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.markAsProcessedAction = markAsProcessedAction;
                this.timeoutsTableName = timeoutsTableName;
                this.id = id;
            }

            public static DuePostgreSqlTimeout Create(Action<DuePostgreSqlTimeout> markAsProcessedAction, string timeoutsTableName, IDataReader reader)
            {
                var id = (long)reader["id"];
                var correlationId = (string)reader["correlation_id"];
                var sagaId = (Guid)reader["saga_id"];
                var replyTo = (string)reader["reply_to"];
                var timeToReturn = (DateTime)reader["time_to_return"];
                var customData = (string)(reader["custom_data"] != DBNull.Value ? reader["custom_data"] : "");

                var timeout = new DuePostgreSqlTimeout(markAsProcessedAction, timeoutsTableName, id, replyTo, correlationId, timeToReturn, sagaId, customData);

                return timeout;
            }

            public override void MarkAsProcessed()
            {
                markAsProcessedAction(this);
            }

            public long Id
            {
                get { return id; }
            }
        }
    }
}