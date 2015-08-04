using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that persists subscriptions in a table in SQL Server
    /// </summary>
    public class SqlServerSubscriptionStorage : ISubscriptionStorage
    {
        static ILog _log;

        static SqlServerSubscriptionStorage()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly DbConnectionProvider _connectionProvider;
        readonly string _tableName;

        /// <summary>
        /// Constructs the storage using the specified connection provider and table to store its subscriptions. If the subscription
        /// storage is shared by all subscribers and publishers, the <paramref name="isCentralized"/> parameter can be set to true
        /// in order to subscribe/unsubscribe directly instead of sending subscription/unsubscription requests
        /// </summary>
        public SqlServerSubscriptionStorage(DbConnectionProvider connectionProvider, string tableName, bool isCentralized)
        {

            IsCentralized = isCentralized;
            _connectionProvider = connectionProvider;
            _tableName = tableName;
        }

        /// <summary>
        /// Creates the subscriptions table if necessary
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionProvider.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(_tableName, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                _log.Info("Table '{0}' does not exist - it will be created now", _tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[topic] [nvarchar](200) NOT NULL,
	[address] [nvarchar](200) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [topic] ASC,
	    [address] ASC
    )
)
", _tableName);
                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format("SELECT [address] FROM [{0}] WHERE [topic] = @topic", _tableName);
                    command.Parameters.Add("topic", SqlDbType.NVarChar, 200).Value = topic;

                    var subscriberAddresses = new List<string>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var address = (string) reader["address"];
                            subscriberAddresses.Add(address);
                        }
                    }

                    return subscriberAddresses.ToArray();
                }                
            }
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

IF NOT EXISTS (SELECT * FROM [{0}] WHERE [topic] = @topic AND [address] = @address)
BEGIN
    INSERT INTO [{0}] ([topic], [address]) VALUES (@topic, @address)
END

", _tableName);
                    command.Parameters.Add("topic", SqlDbType.NVarChar, 200).Value = topic;
                    command.Parameters.Add("address", SqlDbType.NVarChar, 200).Value = subscriberAddress;

                    await command.ExecuteNonQueryAsync();
                }

                await connection.Complete();
            }
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
DELETE FROM [{0}] WHERE [topic] = @topic AND [address] = @address
", _tableName);
                    command.Parameters.Add("topic", SqlDbType.NVarChar, 200).Value = topic;
                    command.Parameters.Add("address", SqlDbType.NVarChar, 200).Value = subscriberAddress;

                    await command.ExecuteNonQueryAsync();
                }

                await connection.Complete();
            }
        }

        public bool IsCentralized { get; private set; }
    }
}