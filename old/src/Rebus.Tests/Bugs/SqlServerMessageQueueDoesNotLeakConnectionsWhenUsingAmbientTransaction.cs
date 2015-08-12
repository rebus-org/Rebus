using System.Collections.Generic;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Persistence;
using Rebus.Transports.Sql;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class SqlServerMessageQueueDoesNotLeakConnectionsWhenUsingAmbientTransaction : SqlServerFixtureBase
    {
        const string InputQueueName = "test.connection.leak";
        const string TableName = "test_connection_leak";

        [Test]
        public void DoesNotLeakConnectionsForEachMessage()
        {
            // Low timeout + pool size will quickly show the issue.
            var cs = ConnectionString + ";Connect Timeout = 5;Max Pool Size=10";
            var transport = new SqlServerMessageQueue(cs, TableName, InputQueueName);
            transport.EnsureTableIsCreated();
            transport.PurgeInputQueue();

            for (int i = 0; i < 100; i++)
            {
                using (var tx = new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    transport.Send(InputQueueName, new TransportMessageToSend() { Body = new byte[0], Headers = new Dictionary<string, object>(), Label = "test" }, ctx);
                    tx.Complete();
                }
            }

        }
    }
}
