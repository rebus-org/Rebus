using System;
using Npgsql;
using NUnit.Framework;
using Rebus.PostgreSql;
using Shouldly;

namespace Rebus.Tests.Persistence.PostgreSql
{
    [TestFixture, Category(TestCategories.PostgreSql)]
    public class TestPostgreSqlSubscriptionStorage_UserProvidedConnection : PostgreSqlFixtureBase
    {
        PostgreSqlSubscriptionStorage storage;
        NpgsqlConnection currentConnection;
        NpgsqlTransaction currentTransaction;
        const string SubscriptionsTableName = "testSubscriptionsTable";

        [Test]
        public void WorksWithUserProvidedConnectionWithStartedTransaction()
        {
            BeginTransaction();

            storage.Store(typeof(string), "whatever");

            CommitTransaction();
        }

        [Test]
        public void WorksWithUserProvidedConnectionWithoutStartedTransaction()
        {
            storage.Store(typeof(string), "whatever");
        }

        [Test]
        public void CanCreateSagaTablesAutomatically()
        {
            storage.EnsureTableIsCreated();

            var existingTables = GetTableNames();
            existingTables.ShouldContain(SubscriptionsTableName);
        }

        protected override void DoSetUp()
        {
            DropTable(SubscriptionsTableName);

            storage = new PostgreSqlSubscriptionStorage(GetOrCreateConnection, SubscriptionsTableName);
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
    }
}