using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Logging;
using Rebus.Transports.Sql;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implements a subscriotion storage for Rebus that will store subscriptions in an SQL Server.
    /// </summary>
    public class SqlServerSubscriptionStorage : SqlServerStorage, IStoreSubscriptions
    {
        static ILog log;

        static SqlServerSubscriptionStorage()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string subscriptionsTableName;

        /// <summary>
        /// Constructs the storage with the ability to create connections to SQL Server using the specified connection string.
        /// This also means that the storage will manage the connection by itself, closing it when it has stopped using it.
        /// </summary>
        public SqlServerSubscriptionStorage(string connectionStringOrConnectionStringName, string subscriptionsTableName)
            : base(connectionStringOrConnectionStringName)
        {
            this.subscriptionsTableName = subscriptionsTableName;
        }

        /// <summary>
        /// Constructs the storage with the ability to use an externally provided <see cref="SqlConnection"/>, thus allowing it
        /// to easily enlist in any ongoing SQL transaction magic that might be going on. This means that the storage will assume
        /// that someone else manages the connection's lifetime.
        /// </summary>
        public SqlServerSubscriptionStorage(Func<ConnectionHolder> connectionFactoryMethod, string subscriptionsTableName)
            : base(connectionFactoryMethod)
        {
            this.subscriptionsTableName = subscriptionsTableName;
        }

        /// <summary>
        /// Returns the name of the table used to store subscriptions
        /// </summary>
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
                    command.CommandText = string.Format(@"if not exists
                                                (select top 1 * from [{0}] where message_type = @message_type and endpoint = @endpoint)
                                                insert into [{0}] 
                                                (message_type, endpoint) 
                                                values (@message_type, @endpoint)", subscriptionsTableName);

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
        /// Removes the subscription (if any) for the given message type and subscriber endpoint from the underlying SQL table.
        /// </summary>
        public void Remove(Type eventType, string subscriberInputQueue)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}]
                                                where message_type = @message_type
                                                and endpoint = @endpoint", subscriptionsTableName);

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
        /// Queries the underlying table for subscriber endpoints that are subscribed to the given message type
        /// </summary>
        public string[] GetSubscribers(Type eventType)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"select endpoint from [{0}]
                                                where message_type = @message_type", subscriptionsTableName);

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
        public SqlServerSubscriptionStorage EnsureTableIsCreated()
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
CREATE TABLE [dbo].[{0}] (
	[message_type] [nvarchar](200) NOT NULL,
	[endpoint] [nvarchar](200) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [message_type] ASC,
	    [endpoint] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
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