using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSubscriptionStorage : IStoreSubscriptions
    {
        const int PrimaryKeyViolationNumber = 2627;
        readonly string connectionString;
        readonly string subscriptionsTableName;

        public SqlServerSubscriptionStorage(string connectionString, string subscriptionsTableName)
        {
            this.connectionString = connectionString;
            this.subscriptionsTableName = subscriptionsTableName;
        }

        public string SubscriptionsTableName
        {
            get { return subscriptionsTableName; }
        }

        public void Store(Type messageType, string subscriberInputQueue)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

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
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

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
        }

        public string[] GetSubscribers(Type messageType)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

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
        }
    }
}