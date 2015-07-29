using System;
using System.Data.SqlClient;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Sql;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSagaPersister_UserProvidedConnection : SqlServerFixtureBase
    {
        SqlServerSagaPersister persister;
        
        SqlConnection currentConnection;
        SqlTransaction currentTransaction;

        protected override void DoSetUp()
        {
            DropSagaTables();

            persister = new SqlServerSagaPersister(GetOrCreateConnection, SagaIndexTableName, SagaTableName);
            persister.EnsureTablesAreCreated();
        }

        protected override void DoTearDown()
        {
            if (currentConnection == null) return;

            currentConnection.Dispose();
            currentConnection = null;
        }

        ConnectionHolder GetOrCreateConnection()
        {
            if (currentConnection != null)
            {
                return currentTransaction == null
                    ? ConnectionHolder.ForNonTransactionalWork(currentConnection)
                    : ConnectionHolder.ForTransactionalWork(currentConnection, currentTransaction);
            }

            var newConnection = new SqlConnection(ConnectionStrings.SqlServer);
            newConnection.Open();
            currentConnection = newConnection;
            
            return ConnectionHolder.ForNonTransactionalWork(newConnection);
        }

        void BeginTransaction()
        {
            if (currentTransaction != null)
            {
                throw new InvalidOperationException("Cannot begin new transaction when a transaction has already been started!");
            }
            currentTransaction = GetOrCreateConnection().Connection.BeginTransaction();
        }

        void CommitTransaction()
        {
            if (currentTransaction == null)
            {
                throw new InvalidOperationException("Cannot commit transaction when no transaction has been started!");
            }
            currentTransaction.Commit();
            currentTransaction = null;
        }

        [Test]
        public void WorksWithUserProvidedConnectionWithStartedTransaction()
        {
            // arrange
            var sagaId = Guid.NewGuid();
            var sagaData = new SomeSagaData { JustSomething = "hey!", Id = sagaId };

            // act
            BeginTransaction();

            // assert
            persister.Insert(sagaData, new string[0]);

            CommitTransaction();
        }

        [Test]
        public void WorksWithUserProvidedConnectionWithoutStartedTransaction()
        {
            // arrange
            var sagaId = Guid.NewGuid();
            var sagaData = new SomeSagaData { JustSomething = "hey!", Id = sagaId };

            // act

            // assert
            persister.Insert(sagaData, new string[0]);

        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string JustSomething { get; set; }
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            // arrange

            // act
            persister.EnsureTablesAreCreated();

            // assert
            var existingTables = GetTableNames();
            existingTables.ShouldContain(SagaIndexTableName);
            existingTables.ShouldContain(SagaTableName);
        }
    }
}