using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus.PostgreSql.Subscriptions
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that uses Postgres to do its thing
    /// </summary>
    public class PostgreSqlSubscriptionStorage : ISubscriptionStorage
    {
        const string UniqueKeyViolation = "23505";
        static ILog _log;

        static PostgreSqlSubscriptionStorage()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly PostgresConnectionHelper _connectionHelper;
        readonly string _tableName;

        /// <summary>
        /// Constructs the subscription storage, storing subscriptions in the specified <paramref name="tableName"/>.
        /// If <paramref name="isCentralized"/> is true, subscribing/unsubscribing will be short-circuited by manipulating
        /// subscriptions directly, instead of requesting via messages
        /// </summary>
        public PostgreSqlSubscriptionStorage(PostgresConnectionHelper connectionHelper, string tableName, bool isCentralized)
        {
            _connectionHelper = connectionHelper;
            _tableName = tableName;
            IsCentralized = isCentralized;
        }

        /// <summary>
        /// Creates the subscriptions table if no table with the specified name exists
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionHelper.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames().ToHashSet();

                if (tableNames.Contains(_tableName)) return;

                _log.Info("Table '{0}' does not exist - it will be created now", _tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE ""{0}"" (
	""topic"" VARCHAR(200) NOT NULL,
	""address"" VARCHAR(200) NOT NULL,
	PRIMARY KEY (""topic"", ""address"")
);
", _tableName);
                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            using (var connection = await _connectionHelper.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"select ""address"" from ""{0}"" where ""topic"" = @topic", _tableName);

                command.Parameters.AddWithValue("topic", NpgsqlDbType.Text, topic);

                var endpoints = new List<string>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        endpoints.Add((string)reader["address"]);
                    }
                }

                return endpoints.ToArray();
            }
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            using(var connection = await _connectionHelper.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"insert into ""{0}"" (""topic"", ""address"") values (@topic, @address)", _tableName);

                command.Parameters.AddWithValue("topic", NpgsqlDbType.Text, topic);
                command.Parameters.AddWithValue("address", NpgsqlDbType.Text, subscriberAddress);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (NpgsqlException exception)
                {
                    if (exception.Code != UniqueKeyViolation) throw;
                }

                connection.Complete();
            }
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            using (var connection = await _connectionHelper.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"delete from ""{0}"" where ""topic"" = @topic and ""address"" = @address;", _tableName);

                command.Parameters.AddWithValue("topic", NpgsqlDbType.Text, topic);
                command.Parameters.AddWithValue("address", NpgsqlDbType.Text, subscriberAddress);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (NpgsqlException exception)
                {
                    Console.WriteLine(exception);
                }

                connection.Complete();
            }
        }

        public bool IsCentralized
        {
            get; private set;
        }
    }
}
