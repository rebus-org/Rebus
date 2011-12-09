// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSubscriptionStorage : IStoreSubscriptions
    {
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
                        if (!ex.Errors.Cast<SqlError>()
                                 .Any(e => e.ToString().Contains("Violation of PRIMARY KEY constraint 'PK_subscriptions'")))
                        {
                            throw;
                        }
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
                            endpoints.Add((string) reader["endpoint"]);
                        }
                    }
                    return endpoints.ToArray();
                }
            }
        }
    }
}