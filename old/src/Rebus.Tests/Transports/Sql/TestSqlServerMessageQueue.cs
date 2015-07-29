using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Persistence;
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
    }
}