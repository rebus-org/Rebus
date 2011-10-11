using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSubscriptionStorage : IStoreSubscriptions
    {
        readonly string connectionString;

        public SqlServerSubscriptionStorage(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Save(Type messageType, string subscriberInputQueue)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"insert into subscriptions 
                                            (message_type, endpoint) 
                                            values (@message_type, @endpoint)";

                command.Parameters.AddWithValue("message_type", messageType.FullName);
                command.Parameters.AddWithValue("endpoint", subscriberInputQueue);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    if (!ex.Errors.Cast<SqlError>()
                        .Any(e => e.ToString().Contains("Violation of PRIMARY KEY constraint 'PK_subscriptions'")))
                    {
                        throw;
                    }
                }
            }
        }

        public string[] GetSubscribers(Type messageType)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"select endpoint from subscriptions 
                                            where message_type = @message_type";

                command.Parameters.AddWithValue("message_type", messageType.FullName);

                var endpoints = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        endpoints.Add((string) reader["endpoint"]);
                    }
                }
                return endpoints.ToArray();
            }
        }
    }
}