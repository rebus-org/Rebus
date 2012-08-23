using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSubscriptionStorage : IStoreSubscriptions
    {
        const int PrimaryKeyViolationNumber = 2627;
        readonly string subscriptionsTableName;

        readonly Func<SqlConnection> getConnection;
        readonly Action<SqlConnection> releaseConnection;

        /// <summary>
        /// Constructs the storage with the ability to create connections to SQL Server using the specified connection string.
        /// This also means that the storage will manage the connection by itself, closing it when it has stopped using it.
        /// </summary>
        public SqlServerSubscriptionStorage(string connectionString, string subscriptionsTableName)
        {
            this.subscriptionsTableName = subscriptionsTableName;

            getConnection = () =>
                {
                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    return connection;
                };
            releaseConnection = c => c.Dispose();
        }

        /// <summary>
        /// Constructs the storage with the ability to use an externally provided <see cref="SqlConnection"/>, thus allowing it
        /// to easily enlist in any ongoing SQL transaction magic that might be going on. This means that the storage will assume
        /// that someone else manages the connection's lifetime.
        /// </summary>
        public SqlServerSubscriptionStorage(Func<SqlConnection> connectionFactoryMethod, string subscriptionsTableName)
        {
            this.subscriptionsTableName = subscriptionsTableName;

            getConnection = connectionFactoryMethod;
            releaseConnection = c => { };
        }

        public string SubscriptionsTableName
        {
            get { return subscriptionsTableName; }
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"insert into [{0}] 
                                                (message_type, endpoint) 
                                                values (@message_type, @endpoint)", subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", messageType.FullName);
                    command.Parameters.AddWithValue("endpoint", subscriberInputQueue);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number != PrimaryKeyViolationNumber) throw;
                    }
                }
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}]
                                                where message_type = @message_type
                                                and endpoint = @endpoint", subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", messageType.FullName);
                    command.Parameters.AddWithValue("endpoint", subscriberInputQueue);

                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        public string[] GetSubscribers(Type messageType)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"select endpoint from [{0}]
                                                where message_type = @message_type", subscriptionsTableName);

                    command.Parameters.AddWithValue("message_type", messageType.FullName);

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
    }
}