using System;
using System.Data.SqlClient;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Tests.Persistence;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSagasUsingMsSqlPersistence : SqlServerFixtureBase
    {
        const string InputQueueName = "test.saga.input";

        protected override void DoSetUp()
        {
            base.DoSetUp();
            DropSagaTables();
        }

        [Test]
        public void CanHandleSagaMessageWithinTransactionScope()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                //Arrange
                adapter.Register(typeof(TestSaga));

                var bus = CreateBus(adapter, InputQueueName);
                bus.Start(1).Subscribe<TestSagaMessage>();
                Thread.Sleep(1000); //Wait for subscription message to be processed

                // Act
                adapter.Bus.Publish(new TestSagaMessage());
                Thread.Sleep(1000); //Wait for message to be processed

                // Assert
                Assert.That(() => ExecuteScalar(string.Format("select count(*) from {0}", SagaTableName)),
                            Is.EqualTo(1).After(12000, 500));
            }
        }

        public override string GetEndpointFor(Type messageType)
        {
            return InputQueueName;
        }

        public class TestSaga : Saga<TestSagaData>, IAmInitiatedBy<TestSagaMessage>
        {
            public void Handle(TestSagaMessage message)
            {
                DoSqlStuffToUseImplicitTransaction();
            }

            static void DoSqlStuffToUseImplicitTransaction()
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "create table #INTEGRATIONTESTING (value nvarchar(255))";
                        command.ExecuteNonQuery();
                    }
                }
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<TestSagaMessage>(x => x.Reference)
                .CorrelatesWith(x => x.Reference);
            }
        }

        public class TestSagaMessage
        {

            public string Reference { get; set; }
        }

        public class TestSagaData : ISagaData
        {
            public TestSagaData()
            {
                DateTime = DateTime.Now;
                Reference = Guid.NewGuid().ToString();
            }
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string Reference { get; set; }
            public DateTime DateTime { get; set; }
        }
    }
}