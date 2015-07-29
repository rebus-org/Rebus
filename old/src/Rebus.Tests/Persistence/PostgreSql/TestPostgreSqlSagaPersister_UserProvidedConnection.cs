using System;
using Npgsql;
using NUnit.Framework;
using Rebus.PostgreSql;
using Shouldly;

namespace Rebus.Tests.Persistence.PostgreSql
{
    [TestFixture, Category(TestCategories.PostgreSql)]
    public class TestPostgreSqlSagaPersister_UserProvidedConnection : PostgreSqlFixtureBase
    {
        PostgreSqlSagaPersister persister;
        
        NpgsqlConnection currentConnection;
        NpgsqlTransaction currentTransaction;

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
            var sagaId = Guid.NewGuid();
            var sagaData = new SomeSagaData { JustSomething = "hey!", Id = sagaId };

            persister.Insert(sagaData, new string[0]);
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            persister.EnsureTablesAreCreated();

            var existingTables = GetTableNames();
            existingTables.ShouldContain(SagaIndexTableName);
            existingTables.ShouldContain(SagaTableName);
        }

        protected override void DoSetUp()
        {
            DropSagaTables();

            persister = new PostgreSqlSagaPersister(GetOrCreateConnection, SagaIndexTableName, SagaTableName);
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

            var newConnection = new NpgsqlConnection(ConnectionString);
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

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string JustSomething { get; set; }
        }
    }
}