using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Persistence.SqlServer;
using Rebus.Transport;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture]
    public class TestSqlServerTransport : FixtureBase
    {
        const string QueueName = "input";
        readonly string _tableName = "messages" + TestConfig.Suffix;
        SqlServerTransport _transport;

        protected override void SetUp()
        {
            SqlTestHelper.DropTable(_tableName);

            _transport = new SqlServerTransport(new DbConnectionProvider(SqlTestHelper.ConnectionString), _tableName, QueueName);
            _transport.EnsureTableIsCreated();

            Using(_transport);

            _transport.Initialize();
        }

        [Test]
        public async Task ReceivesSentMessageWhenTransactionIsCommitted()
        {
            using (var context = new DefaultTransactionContext())
            {
                await _transport.Send(QueueName, RecognizableMessage(), context);

                await context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await _transport.Receive(context);

                await context.Complete();

                AssertMessageIsRecognized(transportMessage);
            }
        }

        [Test]
        public async Task DoesNotReceiveSentMessageWhenTransactionIsNotCommitted()
        {
            using (var context = new DefaultTransactionContext())
            {
                await _transport.Send(QueueName, RecognizableMessage(), context);

                //await context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await _transport.Receive(context);

                Assert.That(transportMessage, Is.Null);
            }
        }

        void AssertMessageIsRecognized(TransportMessage transportMessage)
        {
            Assert.That(transportMessage.Headers.GetValue("recognizzle"), Is.EqualTo("hej"));
        }

        TransportMessage RecognizableMessage()
        {
            var headers = new Dictionary<string, string>
            {
                {"recognizzle", "hej"}
            };
            return new TransportMessage(headers, new byte[] { 1, 2, 3 });
        }
    }
}