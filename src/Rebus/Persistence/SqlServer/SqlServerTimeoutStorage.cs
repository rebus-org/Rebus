using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Timeout;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerTimeoutStorage : IStoreTimeouts
    {
        const int PrimaryKeyViolationNumber = 2627;
        readonly string connectionString;
        readonly string timeoutsTableName;

        public SqlServerTimeoutStorage(string connectionString, string timeoutsTableName)
        {
            this.connectionString = connectionString;
            this.timeoutsTableName = timeoutsTableName;
        }

        public string TimeoutsTableName
        {
            get { return timeoutsTableName; }
        }

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
                                new Tuple<string, object>("reply_to", newTimeout.ReplyTo)
                            };

                    if (newTimeout.CustomData!= null)
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
                    foreach(var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);
                    }
                    
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

        public IEnumerable<Timeout.Timeout> RemoveDueTimeouts()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var dueTimeouts = new List<Timeout.Timeout>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            @"select time_to_return, correlation_id, reply_to, custom_data from [{0}] where time_to_return <= @current_time",
                            timeoutsTableName);

                    command.Parameters.AddWithValue("current_time", Time.Now());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var correlationId = (string) reader["correlation_id"];
                            var replyTo = (string) reader["reply_to"];
                            var timeToReturn = (DateTime) reader["time_to_return"];
                            var customData = (string) (reader["custom_data"] != DBNull.Value ? reader["custom_data"] : "");

                            dueTimeouts.Add(new Timeout.Timeout
                                                {
                                                    CorrelationId = correlationId,
                                                    ReplyTo = replyTo,
                                                    TimeToReturn = timeToReturn,
                                                    CustomData = customData,
                                                });
                        }
                    }

                }

                dueTimeouts.ForEach(t => DeleteTimeout(t, connection));

                return dueTimeouts;
            }
        }

        void DeleteTimeout(Timeout.Timeout timeout, SqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    string.Format(
                        @"delete from [{0}] 
                          where time_to_return = @time_to_return 
                            and reply_to = @reply_to
                            and correlation_id = @correlation_id",
                        timeoutsTableName);

                command.Parameters.AddWithValue("time_to_return", timeout.TimeToReturn);
                command.Parameters.AddWithValue("correlation_id", timeout.CorrelationId);
                command.Parameters.AddWithValue("reply_to", timeout.ReplyTo);

                var executeNonQuery = command.ExecuteNonQuery();

                if (executeNonQuery == 0)
                {
                    throw new InvalidOperationException(string.Format("Stale state! Attempted to delete {0} from {1}, but it was already deleted!", timeout, timeoutsTableName));
                }
            }
        }
    }
}