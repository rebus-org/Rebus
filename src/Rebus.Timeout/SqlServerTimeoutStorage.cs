using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Timeout
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

        public void Add(Timeout newTimeout)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            @"insert into [{0}] (time_to_return, correlation_id, reply_to)
                                        values (@time_to_return, @correlation_id, @reply_to)",
                            timeoutsTableName);

                    command.Parameters.AddWithValue("time_to_return", newTimeout.TimeToReturn);
                    command.Parameters.AddWithValue("correlation_id", newTimeout.CorrelationId);
                    command.Parameters.AddWithValue("reply_to", newTimeout.ReplyTo);

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

        public IEnumerable<Timeout> RemoveDueTimeouts()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var dueTimeouts = new List<Timeout>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            @"select time_to_return, correlation_id, reply_to from [{0}] where time_to_return <= @current_time",
                            timeoutsTableName);

                    command.Parameters.AddWithValue("current_time", Time.Now());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dueTimeouts.Add(new Timeout
                                                {
                                                    CorrelationId = (string)reader["correlation_id"],
                                                    ReplyTo = (string)reader["reply_to"],
                                                    TimeToReturn = (DateTime)reader["time_to_return"]
                                                });
                        }
                    }

                }

                dueTimeouts.ForEach(t => DeleteTimeout(t, connection));

                return dueTimeouts;
            }
        }

        void DeleteTimeout(Timeout timeout, SqlConnection connection)
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