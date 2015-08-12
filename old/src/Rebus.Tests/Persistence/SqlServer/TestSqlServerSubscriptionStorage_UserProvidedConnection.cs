using System;
using System.Data.SqlClient;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Sql;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSubscriptionStorage_UserProvidedConnection : SqlServerFixtureBase
    {
        SqlServerSubscriptionStorage storage;
        SqlConnection currentConnection;
        SqlTransaction currentTransaction;
        const string SubscriptionsTableName = "testSubscriptionsTable";

        protected override void DoSetUp()
        {
            // ensure the two tables are dropped
            try { ExecuteCommand("drop table " + SubscriptionsTableName); }
            catch { }

            storage = new SqlServerSubscriptionStorage(GetOrCreateConnection, SubscriptionsTableName);
            storage.EnsureTableIsCreated();
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
            BeginTransaction();

            // act
            storage.Store(typeof(string), "whatever");

            // assert
            CommitTransaction();
        }

        [Test]
        public void WorksWithUserProvidedConnectionWithoutStartedTransaction()
        {
            // arrange

            // act
            storage.Store(typeof(string), "whatever");

            // assert
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            // arrange

            // act
            storage.EnsureTableIsCreated();

            // assert
            var existingTables = GetTableNames();
            existingTables.ShouldContain(SubscriptionsTableName);
        }
    }
}