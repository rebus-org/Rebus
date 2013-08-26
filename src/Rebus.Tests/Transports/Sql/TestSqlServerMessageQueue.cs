using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Persistence;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Sql;
using Shouldly;

namespace Rebus.Tests.Transports.Sql
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerMessageQueue : SqlServerFixtureBase
    {
        const string InputQueueName = "test";
        SqlServerMessageQueue queue;

        protected override void DoSetUp()
        {
            if (GetTableNames()
                .Contains("messages"))
            {
                ExecuteCommand("drop table [messages]");
            }

            queue = new SqlServerMessageQueue(ConnectionString, "messages", InputQueueName)
                .EnsureTableIsCreated()
                .PurgeInputQueue();
        }

        [Test]
        public void ReceivesMessagesInPrioritizedOrder()
        {
            // expect reverse order because of priorities
            queue.Send(InputQueueName, MessageWith("msg1", 3), new NoTransaction());
            queue.Send(InputQueueName, MessageWith("msg2", 2), new NoTransaction());
            queue.Send(InputQueueName, MessageWith("msg3", 1), new NoTransaction());

            var receivedContents1 = ExtractContents(queue.ReceiveMessage(new NoTransaction()));
            var receivedContents2 = ExtractContents(queue.ReceiveMessage(new NoTransaction()));
            var receivedContents3 = ExtractContents(queue.ReceiveMessage(new NoTransaction()));

            receivedContents1.ShouldBe("msg3");
            receivedContents2.ShouldBe("msg2");
            receivedContents3.ShouldBe("msg1");
        }

        string ExtractContents(ReceivedTransportMessage receiveMessage)
        {
            return Encoding.UTF8.GetString(receiveMessage.Body);
        }

        TransportMessageToSend MessageWith(string contents, int priority)
        {
            return
                new TransportMessageToSend
                    {
                        Body = Encoding.UTF8.GetBytes(contents),
                        Headers =
                            new Dictionary<string, object>
                                {
                                    {SqlServerMessageQueue.PriorityHeaderKey, priority}
                                },
                        Label = contents
                    };
        }

        [Test, Ignore("Was only used to demonstrate that the approach taken in SqlServerMessageQueue was viable")]
        public void CanDoItManually()
        {
            string id = null;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                connection.BeginTransaction();

                using (var c = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(c);
                    c.CommandText = "delete from messages";
                    c.ExecuteNonQuery();
                }

                connection.GetTransactionOrNull()
                          .Commit();
            }

            using (new TransactionScope())
            {
                var tx2 = new TransactionScope(TransactionScopeOption.Suppress);
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    connection.BeginTransaction();

                    tx2.Dispose();

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "1");
                        c.Parameters.AddWithValue("data", "hej1");
                        c.ExecuteNonQuery();
                    }

                    connection.GetTransactionOrNull()
                              .Commit();
                }

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    connection.BeginTransaction();

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "select top 1 id, data from messages with (updlock, readpast)";
                        using (var reader = c.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                id = (string)reader["id"];
                            }
                        }
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "2");
                        c.Parameters.AddWithValue("data", "hej2");
                        c.ExecuteNonQuery();
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "delete from messages where id = @id";
                        c.Parameters.AddWithValue("id", id);
                        c.ExecuteNonQuery();
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "3");
                        c.Parameters.AddWithValue("data", "hej3");
                        c.ExecuteNonQuery();
                    }

                    connection.GetTransactionOrNull()
                              .Rollback();
                }
            }
        }
    }
}