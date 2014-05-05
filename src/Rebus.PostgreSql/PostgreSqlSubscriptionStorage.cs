using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Rebus.Logging;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Implements a subscription storage for Rebus that stores sagas in PostgreSql.
    /// </summary>
    public class PostgreSqlSubscriptionStorage : PostgreSqlStorage, IStoreSubscriptions
    {
        static ILog log;

        readonly string subscriptionsTableName;

        static PostgreSqlSubscriptionStorage()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public PostgreSqlSubscriptionStorage(Func<ConnectionHolder> connectionFactoryMethod, string subscriptionsTableName)
            : base(connectionFactoryMethod)
        {
            this.subscriptionsTableName = subscriptionsTableName;
        }

        public PostgreSqlSubscriptionStorage(string connectionString, string subscriptionsTableName)
            : base(connectionString)
        {
            this.subscriptionsTableName = subscriptionsTableName;
        }

        public string SubscriptionsTableName
        {
            get { return subscriptionsTableName; }
        }

        /// <summary>
        /// Stores a subscription for the given message type and the given subscriber endpoint in the underlying SQL table.
        /// </summary>
        public void Store(Type eventType, string subscriberInputQueue)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    const string Sql = @"insert into ""{0}"" (""message_type"", ""endpoint"") values (@message_type, @endpoint)";

                    command.CommandText = string.Format(Sql, subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", eventType.FullName);
                    command.Parameters.AddWithValue("endpoint", subscriberInputQueue);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (NpgsqlException)
                    {
                    }
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Removes the subscription (if any) for the given message type and subscriber endpoint from the underlying SQL table.
        /// </summary>
        public void Remove(Type eventType, string subscriberInputQueue)
        {
            const string Sql = @"delete from ""{0}"" where ""message_type"" = @message_type and ""endpoint"" = @endpoint";

            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(Sql, subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", eventType.FullName);
                    command.Parameters.AddWithValue("endpoint", subscriberInputQueue);

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
        /// Queries the underlying table for subscriber endpoints that are subscribed to the given message type.
        /// </summary>
        public string[] GetSubscribers(Type eventType)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    const string Sql = @"select ""endpoint"" from ""{0}"" where ""message_type"" = @message_type";

                    command.CommandText = string.Format(Sql, subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", eventType.FullName);

                    var endpoints = new List<string>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            endpoints.Add((string)reader["endpoint"]);
                        }
                    }

                    return endpoints.ToArray();
                }
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Creates the necessary subscripion storage table if it hasn't already been created. If a table already exists
        /// with a name that matches the desired table name, no action is performed (i.e. it is assumed that
        /// the table already exists).
        /// </summary>
        public PostgreSqlSubscriptionStorage EnsureTableIsCreated()
        {
            var connection = getConnection();
            try
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(subscriptionsTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }

                log.Info("Table '{0}' does not exist - it will be created now", subscriptionsTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE ""{0}"" (
	""message_type"" VARCHAR(200) NOT NULL,
	""endpoint"" VARCHAR(200) NOT NULL,
	PRIMARY KEY (""message_type"", ""endpoint"")
);
", subscriptionsTableName);
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